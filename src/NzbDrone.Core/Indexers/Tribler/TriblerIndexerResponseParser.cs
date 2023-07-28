using System;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers.Tribler
{
    public interface ITriblerIndexerResponseParser
    {
        TorrentInfo ParseTorrent(ProviderDefinition providerDefinition, TorrentMetadata torrent);
    }

    public class TriblerIndexerResponseParser : ITriblerIndexerResponseParser
    {
        private readonly long _TORRENT_INVENTION_UNIX_TIMESTAMP = 946684800;

        public TorrentInfo ParseTorrent(ProviderDefinition providerDefinition, TorrentMetadata torrent)
        {
            var release = new TorrentInfo
            {
                Title = torrent.Name,
                Seeders = torrent.Num_seeders,
                Peers = torrent.Num_seeders + torrent.Num_leechers,
                InfoHash = torrent.Infohash,
                DownloadProtocol = DownloadProtocol.Torrent,
                Indexer = providerDefinition.Name,
                IndexerId = providerDefinition.Id,
            };

            if (torrent.Size.HasValue)
            {
                release.Size = torrent.Size.Value;
            }

            // drop invalid date's.
            if (torrent.Created.HasValue && torrent.Created.Value > _TORRENT_INVENTION_UNIX_TIMESTAMP)
            {
                release.PublishDate = DateTimeOffset.FromUnixTimeSeconds(torrent.Created.Value).DateTime;
            }

            if (torrent.Updated.HasValue && torrent.Updated.Value > _TORRENT_INVENTION_UNIX_TIMESTAMP)
            {
                release.PublishDate = DateTimeOffset.FromUnixTimeSeconds(torrent.Updated.Value).DateTime;
            }

            release.MagnetUrl = string.Format("magnet:?xt=urn:btih:{0}&dn={1}", torrent.Infohash, torrent.Name);

            if (torrent.Size.HasValue)
            {
                release.MagnetUrl += string.Format("&xl={0}", torrent.Size.Value);
            }

            release.DownloadUrl = release.MagnetUrl;

            release.Guid = "tribler-" + torrent.Id.ToString();

            return release;
        }
    }
}
