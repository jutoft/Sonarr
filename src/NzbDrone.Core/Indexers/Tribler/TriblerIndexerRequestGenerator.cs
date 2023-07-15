using System.Collections.Generic;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Tribler
{
    public interface ITriblerIndexerRequestGenerator
    {
        IList<ReleaseInfo> FetchRecent(TriblerIndexerSettings settings);
        IList<ReleaseInfo> FetchSubscribed(TriblerIndexerSettings settings);
        IList<ReleaseInfo> FetchAll(TriblerIndexerSettings settings, Queue<TriblerChannelSubscription> channels);

        IList<ReleaseInfo> Search(TriblerIndexerSettings settings, string query);
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

        public IList<ReleaseInfo> FetchRecent(TriblerIndexerSettings settings)
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

            return FetchAll(settings, channelQueue);
        }

        /// <summary>
        /// Lookup the tribler client's subscribed channels and visit them one-by-one.
        /// </summary>
        /// <returns></returns>
        public IList<ReleaseInfo> FetchSubscribed(TriblerIndexerSettings settings)
        {
            var channelQueue = new Queue<TriblerChannelSubscription>(_indexerProxy.ChannelsSubscribed(settings));

            return FetchAll(settings, channelQueue);
        }

        public IList<ReleaseInfo> Search(TriblerIndexerSettings settings, string query)
        {
            var torrentInfo = new List<ReleaseInfo>();
            var channelQueue = new Queue<TriblerChannelSubscription>();
            var visitedChannels = new HashSet<TriblerChannelSubscription>();

            // first iterate the initial search.
            foreach (var searchItem in _indexerProxy.Search(settings, query))
            {
                switch (searchItem.Type)
                {
                    // found a torrent, return that with release info.
                    case TriblerMetadataType.RegularTorrent:
                        torrentInfo.Add(_responseParser.ParseTorrent(searchItem));
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

                        // default: other types can just be skipped.
                }
            }

            // then use an ordinary ChannelWalk for the channels etc found.
            torrentInfo.AddRange(FetchAll(settings, channelQueue));

            return torrentInfo;
        }

        public IList<ReleaseInfo> FetchAll(TriblerIndexerSettings settings, Queue<TriblerChannelSubscription> channelQueue)
        {
            IList<ReleaseInfo> torrentInfo = new List<ReleaseInfo>();

            var visitedChannels = new HashSet<TriblerChannelSubscription>(channelQueue);

            while (channelQueue.Count > 0)
            {
                // take the next channel to visit
                var channel = channelQueue.Dequeue();

                // visit channel
                ChannelNested(settings, channel, torrentInfo, visitedChannels, channelQueue);
            }

            return torrentInfo;
        }

        private void ChannelNested(TriblerIndexerSettings settings, TriblerChannelSubscription channel, IList<ReleaseInfo> torrentInfo, ISet<TriblerChannelSubscription> visitedChannels, Queue<TriblerChannelSubscription> channelsToVisit)
        {
            foreach (var channelItem in _indexerProxy.Channel(settings, channel.PublicKey, channel.ChannelId))
            {
                switch (channelItem.Type)
                {
                    // found a torrent, return that with release info.
                    case TriblerMetadataType.RegularTorrent:
                        torrentInfo.Add(_responseParser.ParseTorrent(channelItem));
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

                    // default: other types can just be skipped.
                }
            }
        }
    }
}
