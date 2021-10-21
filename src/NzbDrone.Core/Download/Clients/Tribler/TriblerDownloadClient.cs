using System;
using System.Text.RegularExpressions;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NLog;
using FluentValidation.Results;
using NzbDrone.Core.MediaFiles.TorrentInfo;
using NzbDrone.Core.RemotePathMappings;
using Tribler.Api;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;
using MonoTorrent;

namespace NzbDrone.Core.Download.Clients.Tribler
{
    using TriblerApi = global::Tribler.Api;

    public class TriblerDownloadClient : TorrentClientBase<TriblerDownloadSettings>
    {
        private readonly Lazy<ITriblerApiClient> _triblerApiClient;

        public TriblerDownloadClient(ITorrentFileInfoReader torrentFileInfoReader,
                            IHttpClient httpClient,
                            IConfigService configService,
                            IDiskProvider diskProvider,
                            IRemotePathMappingService remotePathMappingService,
                            Logger logger)
            : base(torrentFileInfoReader, httpClient, configService, diskProvider, remotePathMappingService, logger)
        {
            this._triblerApiClient = new Lazy<ITriblerApiClient>(CreateClient);

        }

        public override string Name => "Tribler";

        public override bool PreferTorrentFile => false;

        private System.Net.Http.HttpClient _httpClient;


        protected ITriblerApiClient CreateClient()
        {
            _httpClient = new System.Net.Http.HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("X-Api-Key", Settings.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", Settings.ApiKey);
            return new TriblerApiClient(Settings.RootUrl, _httpClient);
        }

        public override IEnumerable<DownloadClientItem> GetItems()
        {
            var configAsync = _triblerApiClient.Value.SettingsGETAsync();

            var items = new List<DownloadClientItem>();

            DownloadsResponse downloads = _triblerApiClient.Value.GetDownloadsAsync().Result;

            foreach (var download in downloads.Downloads)
            {

                // If totalsize == 0 the torrent is a magnet downloading metadata
                if (download.Size == null || download.Size == 0) continue;

                var item = new DownloadClientItem();
                item.DownloadId = InfoHash.FromHex(download.Infohash).ToHex();
                //item.Category = Settings.TvCategory;
                item.Title = download.Name;

                item.DownloadClientInfo = DownloadClientItemClientInfo.FromDownloadClient(this);

                var files = _triblerApiClient.Value.FilesAsync(download.Infohash).Result.Files;

                item.OutputPath = new OsPath(download.Destination);

                if (files.Count == 1)
                {
                    item.OutputPath += files.First().Name;
                } else
                {
                    item.OutputPath += item.Title;
                }

                item.TotalSize = (long)download.Size;
                item.RemainingSize = (long)(download.Size * (1 - download.Progress)); // TODO: i expect progress to be between 0 and 1
                item.SeedRatio = download.Ratio;

                if (download.Eta.HasValue)
                {
                    item.RemainingTime = TimeSpan.FromSeconds(download.Eta.Value); // 
                }

                //TODO: the item's message should not be equal to Error
                item.Message = download.Error;

                // tribler always saves files unencrypted to disk.
                item.IsEncrypted = false;

                // state handling

                // TODO: impossible states?
                //Failed = 4,
                //Warning = 5


                //Queued = 0,
                //Completed = 3,
                //Downloading = 2,

                // Paused is guesstimated
                //Paused = 1,


                switch (download.Status)
                {
                    case DownloadStatus.DLSTATUS_HASHCHECKING:
                    case DownloadStatus.DLSTATUS_WAITING4HASHCHECK:
                    case DownloadStatus.DLSTATUS_DOWNLOADING:
                        item.Status = DownloadItemStatus.Downloading;
                        break;
                    case DownloadStatus.DLSTATUS_METADATA:
                    case DownloadStatus.DLSTATUS_ALLOCATING_DISKSPACE:
                        item.Status = DownloadItemStatus.Queued;
                        break;
                    case DownloadStatus.DLSTATUS_SEEDING:
                    case DownloadStatus.DLSTATUS_STOPPED:
                        item.Status = DownloadItemStatus.Completed;
                        break;
                }

                // override status' if completed but progress is not finished
                if (download.Status == DownloadStatus.DLSTATUS_STOPPED && download.Progress < 1)
                {
                    item.Status = DownloadItemStatus.Paused;
                }


                // override status if error is set
                if (download.Error != null && download.Error.Length > 0)
                {
                    item.Status = DownloadItemStatus.Warning; // maybe this should be an error?
                }

                // done (finished seeding & stopped, guessed)
                item.CanBeRemoved = HasReachedSeedLimit(download, configAsync);

                // seeding or done, or stopped
                item.CanMoveFiles = download.Progress == 1.0;

                items.Add(item);
            }

            return items;
        }

        /**
         * this basically checks if torrent is stopped because of seeding has finished
         */
        protected bool HasReachedSeedLimit(TriblerApi.Download torrent, System.Threading.Tasks.Task<GetTriblerSettingsResponse> config)
        {
            // if download is still running then it's not finished.
            if (torrent.Status != DownloadStatus.DLSTATUS_STOPPED)
            {
                return false;
            }

            switch (config.Result.Settings.Download_defaults.Seeding_mode)
            {
                // if in ratio mode, wait for ratio to become larger than expeced. Tribler's DownloadStatus will switch from DLSTATUS_SEEDING to DLSTATUS_STOPPED
                case Download_defaultsSeeding_mode.Ratio:

                    return torrent.Ratio.HasValue
                        && torrent.Ratio >= config.Result.Settings.Download_defaults.Seeding_ratio;

                case Download_defaultsSeeding_mode.Time:
                    var downloadStarted = DateTimeOffset.FromUnixTimeSeconds(torrent.Time_added.Value);
                    var maxSeedingTime = TimeSpan.FromSeconds(config.Result.Settings.Download_defaults.Seeding_time ?? 0);

                    return torrent.Time_added.HasValue
                        && downloadStarted.Add(maxSeedingTime) < DateTimeOffset.Now;

                case Download_defaultsSeeding_mode.Never:
                    return true;

                case Download_defaultsSeeding_mode.Forever:
                default:
                    return false;
            }
        }

        public override void RemoveItem(DownloadClientItem item, bool deleteData)
        {
            //_proxy.RemoveTorrent(item.DownloadId.ToLower(), deleteData, Settings);
            var deleteDownloadRequest = new RemoveDownloadRequest
            {
                Remove_data = deleteData
            };
            _triblerApiClient.Value.RemoveDownloadAsync(item.DownloadId.ToLower(), deleteDownloadRequest).Wait();
        }

        public override DownloadClientInfo GetStatus()
        {
            var config = _triblerApiClient.Value.SettingsGETAsync().Result;
            var destDir = config.Settings.Download_defaults.Saveas;

            if (Settings.TvCategory.IsNotNullOrWhiteSpace())
            {
                destDir = string.Format("{0}/.{1}", destDir, Settings.TvCategory);
            }

            return new DownloadClientInfo
            {
                IsLocalhost = new Uri(Settings.RootUrl).Host == "127.0.0.1" || new Uri(Settings.RootUrl).Host == "localhost",
                OutputRootFolders = new List<OsPath> { _remotePathMappingService.RemapRemoteToLocal(new Uri(Settings.RootUrl).Host, new OsPath(destDir)) }
            };
        }

        protected override string AddFromMagnetLink(RemoteEpisode remoteEpisode, string hash, string magnetLink)
        {
            AddDownloadRequest addDownloadRequest = new AddDownloadRequest();
            addDownloadRequest.Destination = GetDownloadDirectory();
            addDownloadRequest.Uri = magnetLink;
            addDownloadRequest.Safe_seeding = Settings.SafeSeeding;
            addDownloadRequest.Anon_hops = Settings.AnonymityLevel;

            // run hash through InfoHash class to ensure the correct casing.
            string infoHashAsString = _triblerApiClient.Value.AddDownloadAsync(addDownloadRequest).Result.Infohash;
            InfoHash infoHash = MonoTorrent.InfoHash.FromHex(infoHashAsString);
            return infoHash.ToHex();
        }

        protected override string AddFromTorrentFile(RemoteEpisode remoteEpisode, string hash, string filename, byte[] fileContent)
        {
            // TODO: tribler can actually not take a file over the rest api.
            AddDownloadRequest addDownloadRequest = new AddDownloadRequest();
            addDownloadRequest.Destination = GetDownloadDirectory();
            addDownloadRequest.Uri = filename;
            addDownloadRequest.Safe_seeding = Settings.SafeSeeding;
            addDownloadRequest.Anon_hops = Settings.AnonymityLevel;

            // run hash through InfoHash class to ensure the correct casing.
            string infoHashAsString = _triblerApiClient.Value.AddDownloadAsync(addDownloadRequest).Result.Infohash;
            InfoHash infoHash = MonoTorrent.InfoHash.FromHex(infoHashAsString);
            return infoHash.ToHex();
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            if (failures.HasErrors()) return;
            //failures.AddIfNotNull(TestGetTorrents());
        }

        protected string GetDownloadDirectory()
        {
            if (Settings.TvDirectory.IsNotNullOrWhiteSpace())
            {
                return Settings.TvDirectory;
            }

            if (!Settings.TvCategory.IsNotNullOrWhiteSpace()) return null;

            var config = _triblerApiClient.Value.SettingsGETAsync().Result;
            var destDir = config.Settings.Download_defaults.Saveas;

            return $"{destDir.TrimEnd('/')}/{Settings.TvCategory}";
        }

        protected ValidationFailure TestConnection()
        {
            try
            {
                var downloads = _triblerApiClient.Value.GetDownloadsAsync().Result;
                return null;
            }
            catch (DownloadClientAuthenticationException ex)
            {
                _logger.Error(ex, ex.Message);
                return new NzbDroneValidationFailure("ApiKey", "Authentication failure")
                {
                    DetailedDescription = string.Format("Please verify your ApiKey is correct. Also verify if the host running Sonarr isn't blocked from accessing {0} by WhiteList limitations in the {0} configuration.", Name)
                };
            }
            catch (DownloadClientUnavailableException ex)
            {
                _logger.Error(ex, ex.Message);

                return new NzbDroneValidationFailure("Url", "Unable to connect to Tribler")
                {
                    DetailedDescription = ex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to test");

                return new NzbDroneValidationFailure(string.Empty, "Unknown exception: " + ex.Message);
            }
        }

        private ValidationFailure TestGetTorrents()
        {
            try
            {
                _triblerApiClient.Value.GetDownloadsAsync().Wait();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get torrents");
                return new NzbDroneValidationFailure(string.Empty, "Failed to get the list of torrents: " + ex.Message);
            }

            return null;
        }
    }
}
