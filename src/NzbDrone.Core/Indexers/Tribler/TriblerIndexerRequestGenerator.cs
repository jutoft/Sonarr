using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers.Tribler
{
    public interface ITriblerIndexerRequestGenerator
    {
        IList<ReleaseInfo> FetchRecent(ProviderDefinition providerDefinition, TriblerIndexerSettings settings);
        IList<ReleaseInfo> FetchSubscribed(ProviderDefinition providerDefinition, TriblerIndexerSettings settings);
        IList<ReleaseInfo> FetchAll(ProviderDefinition providerDefinition, TriblerIndexerSettings settings, Queue<TriblerChannelSubscription> channels);
        IList<ReleaseInfo> Search(ProviderDefinition providerDefinition, TriblerIndexerSettings settings, string query, int? maxRows = null);
        void StartRemoteQuery(TriblerIndexerSettings settings, string query);
    }

    public class TriblerIndexerRequestGenerator : ITriblerIndexerRequestGenerator
    {
        private ITriblerIndexerProxy _indexerProxy;
        private ITriblerIndexerResponseParser _responseParser;

        public TriblerIndexerRequestGenerator(ITriblerIndexerProxy indexerProxy, ITriblerIndexerResponseParser responseParser)
        {
            this._indexerProxy = indexerProxy;
            this._responseParser = responseParser;
        }

        public IList<ReleaseInfo> FetchRecent(ProviderDefinition providerDefinition, TriblerIndexerSettings settings)
        {
            var channelQueue = new Queue<TriblerChannelSubscription>();

            if (settings.FetchSubscribedChannels)
            {
                foreach (var subscription in _indexerProxy.ChannelsSubscribed(settings))
                {
                    channelQueue.Enqueue(subscription);
                }
            }

            if (settings.FetchExtraChannels)
            {
                foreach (var subscription in settings.GetExtraChannelSubscriptions())
                {
                    channelQueue.Enqueue(subscription);
                }
            }

            return FetchAll(providerDefinition, settings, channelQueue);
        }

        /// <summary>
        /// Lookup the tribler client's subscribed channels and visit them one-by-one.
        /// </summary>
        /// <returns></returns>
        public IList<ReleaseInfo> FetchSubscribed(ProviderDefinition providerDefinition, TriblerIndexerSettings settings)
        {
            var channelQueue = new Queue<TriblerChannelSubscription>(_indexerProxy.ChannelsSubscribed(settings));

            return FetchAll(providerDefinition, settings, channelQueue);
        }

        public IList<ReleaseInfo> Search(ProviderDefinition providerDefinition, TriblerIndexerSettings settings, string query, int? maxRows = null)
        {
            var torrentInfo = new List<ReleaseInfo>();
            var channelQueue = new Queue<TriblerChannelSubscription>();
            var visitedChannels = new HashSet<TriblerChannelSubscription>();

            var fts_query = SanitizeQuery(query);

            StartRemoteQuery(settings, fts_query);

            // TODO: This could be replaced by starting to listen to events before remote query is triggered and then only wait for a few responses to come back.
            // If this delay is removed then the user usually has to do a search 2 times before getting results as the async responses do not get back before we look them up.
            Thread.Sleep(3000); // allow network queries to respond

            // first iterate the initial search.
            foreach (var searchItem in _indexerProxy.Search(settings, fts_query, maxRows))
            {
                switch (searchItem.Type)
                {
                    // found a torrent, return that with release info.
                    case TriblerMetadataType.RegularTorrent:
                        torrentInfo.Add(_responseParser.ParseTorrent(providerDefinition, searchItem));
                        break;

                    // content is another channel
                    case TriblerMetadataType.ChannelNode:
                    case TriblerMetadataType.CollectionNode:
                    case TriblerMetadataType.ChannelTorrent:
                        // recurse & call the endpoint with public key & id here.
                        var subChannelItem = new TriblerChannelSubscription() { PublicKey = searchItem.PublicKey, ChannelId = searchItem.Id.ToString() };
                        if (!visitedChannels.Contains(subChannelItem))
                        {
                            visitedChannels.Add(subChannelItem);
                            channelQueue.Enqueue(subChannelItem);
                        }

                        break;
                    case TriblerMetadataType.Snippet:
                        // new somwhat hidden alternative response type where torrents are grouped, not described in swagger
                        foreach (var subItem in searchItem.TorrentsInSnippets)
                        {
                            torrentInfo.Add(_responseParser.ParseTorrent(providerDefinition, subItem));
                        }

                        break;

                        // default: other types can just be skipped.
                }
            }

            // then use an ordinary ChannelWalk for the channels etc found.
            torrentInfo.AddRange(FetchAll(providerDefinition, settings, channelQueue));

            return torrentInfo;
        }

        public IList<ReleaseInfo> FetchAll(ProviderDefinition providerDefinition, TriblerIndexerSettings settings, Queue<TriblerChannelSubscription> channelQueue)
        {
            IList<ReleaseInfo> torrentInfo = new List<ReleaseInfo>();

            var visitedChannels = new HashSet<TriblerChannelSubscription>(channelQueue);

            while (channelQueue.Count > 0)
            {
                // take the next channel to visit
                var channel = channelQueue.Dequeue();

                // visit channel
                ChannelNested(providerDefinition, settings, channel, torrentInfo, visitedChannels, channelQueue);
            }

            return torrentInfo;
        }

        public static string SanitizeQuery(string query)
        {
            var fts_reg = new Regex(@"[^\w]");
            var duplicate_space_reg = new Regex(@"\s+");
            var fts_query = fts_reg.Replace(query, " ");
            fts_query = duplicate_space_reg.Replace(fts_query, " ").Trim();
            return fts_query;
        }

        private void ChannelNested(ProviderDefinition providerDefinition, TriblerIndexerSettings settings, TriblerChannelSubscription channel, IList<ReleaseInfo> torrentInfo, ISet<TriblerChannelSubscription> visitedChannels, Queue<TriblerChannelSubscription> channelsToVisit)
        {
            foreach (var channelItem in _indexerProxy.Channel(settings, channel.PublicKey, channel.ChannelId))
            {
                switch (channelItem.Type)
                {
                    // found a torrent, return that with release info.
                    case TriblerMetadataType.RegularTorrent:
                        torrentInfo.Add(_responseParser.ParseTorrent(providerDefinition, channelItem));
                        break;

                    // content is another channel
                    case TriblerMetadataType.ChannelNode:
                    case TriblerMetadataType.CollectionNode:
                    case TriblerMetadataType.ChannelTorrent:
                        // recurse & call the endpoint with public key & id here.
                        var subChannelItem = new TriblerChannelSubscription() { PublicKey = channelItem.PublicKey, ChannelId = channelItem.Id.ToString() };
                        if (!visitedChannels.Contains(subChannelItem))
                        {
                            visitedChannels.Add(subChannelItem);
                            channelsToVisit.Enqueue(subChannelItem);
                        }

                        break;

                    case TriblerMetadataType.Snippet:
                        // new somwhat hidden alternative response type where torrents are grouped, not described in swagger
                        foreach (var subItem in channelItem.TorrentsInSnippets)
                        {
                            torrentInfo.Add(_responseParser.ParseTorrent(providerDefinition, subItem));
                        }

                        break;

                        // default: other types can just be skipped.
                }
            }
        }

        public void StartRemoteQuery(TriblerIndexerSettings settings, string query)
        {
            _indexerProxy.StartRemoteQuery(settings, query);
        }
    }
}
