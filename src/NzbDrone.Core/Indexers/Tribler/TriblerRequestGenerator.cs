using System.Collections.Generic;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using Tribler.Api;

namespace NzbDrone.Core.Indexers.Tribler
{
    public class TriblerRequestGenerator : ITriblerSearchRequestGenerator
    {
        private readonly ITriblerApiClient triblerApiClient;
        private readonly long TORRENT_INVENTION_UNIX_TIMESTAMP = 946684800;

        public TriblerIndexerSettings Settings { get; set; }

        public TriblerRequestGenerator(ITriblerApiClient triblerApiClient)
        {
            this.triblerApiClient = triblerApiClient;
        }

        public IList<ReleaseInfo> Search(string query)
        {
            IList<ReleaseInfo> releases = new List<ReleaseInfo>();

            foreach (var torrent in GetPagedRequests(query))
            {
                // currently i have no idea how to fill out TvdbId, TvRageId, ImdbId
                var release = new TorrentInfo
                {
                    Title = torrent.Name,
                    Seeders = torrent.Num_seeders,
                    Peers = torrent.Num_leechers,
                    InfoHash = torrent.Infohash,
                    DownloadProtocol = DownloadProtocol.Torrent,

                };

                if (torrent.Size.HasValue)
                {
                    release.Size = torrent.Size.Value;

                }

                if (torrent.Date.HasValue)
                {
                    release.PublishDate = System.DateTimeOffset.FromUnixTimeSeconds(torrent.Date.Value).DateTime;
                } else if(torrent.Updated.HasValue && torrent.Updated.Value > TORRENT_INVENTION_UNIX_TIMESTAMP) // drop invalid date's. 
                {
                    release.PublishDate = System.DateTimeOffset.FromUnixTimeSeconds(torrent.Updated.Value).DateTime;
                }

                release.MagnetUrl = string.Format("magnet:?xt=urn:btih:{0}&dn={1}", torrent.Infohash, torrent.Name);

                if(torrent.Size.HasValue)
                {
                    release.MagnetUrl += string.Format("&xl={0}", torrent.Size.Value);
                }

                release.DownloadUrl = release.MagnetUrl;

                release.Guid = "tribler-" + torrent.Id.GetValueOrDefault(0).ToString();

                releases.Add(release);

            }

            return releases;
        }

        private IEnumerable<Torrent> GetPagedRequests(string query)
        {
            // possible improvements
            //  - metadata_type could be "Video" to limit returned info

            int results = 1;
            int? first = null;
            int? pageSize = null;

            do
            {
                int? last = null;

                if(first.HasValue && pageSize.HasValue)
                {
                    last = first + pageSize;
                }

                var triblerSearchResponse = triblerApiClient.SearchAsync(first, null, null, null, null, last, null, query).Result;
                first = triblerSearchResponse.Last + 1;

                // save results count so loop stops when no more data is returned.
                results = triblerSearchResponse.Results.Count;

                // use the first response of Last the future page size.
                if(!pageSize.HasValue)
                {
                    pageSize = triblerSearchResponse.Last;

                }

                foreach (var torrent in triblerSearchResponse.Results)
                {
                    yield return torrent;
                }

            } while (results > 0);
        }
    }
}
