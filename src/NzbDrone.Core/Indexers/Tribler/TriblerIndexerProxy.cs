using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Indexers.Tribler
{
    public interface ITriblerIndexerProxy
    {
        IEnumerable<TorrentMetadata> Channel(TriblerIndexerSettings settings, string publicKey, string channelId);
        IEnumerable<TriblerChannelSubscription> ChannelsSubscribed(TriblerIndexerSettings settings);
        IEnumerable<TorrentMetadata> Search(TriblerIndexerSettings settings, string query, int? maxRows = null);
        RemoteSearchResponse StartRemoteQuery(TriblerIndexerSettings settings, string query);
    }

    public class TriblerIndexerProxy : ITriblerIndexerProxy
    {
        private IHttpClient _httpClient;

        public TriblerIndexerProxy(IHttpClient httpClient)
        {
            this._httpClient = httpClient;
        }

        private SearchResponse GetChannel(TriblerIndexerSettings settings, string publicKey, string channelId, int first)
        {
            var requestBuilder = new HttpRequestBuilder(settings.BaseUrl + "/channels" + "/" + publicKey + "/" + channelId).Accept(HttpAccept.Json);

            requestBuilder.Headers.Add("X-Api-Key", settings.ApiKey);

            // start index
            requestBuilder.AddQueryParam("first", first.ToString());

            var response = _httpClient.Execute(requestBuilder.Build());

            return Json.Deserialize<SearchResponse>(response.Content);
        }

        private SearchResponse GetSubscribedChannels(TriblerIndexerSettings settings, int first)
        {
            var requestBuilder = new HttpRequestBuilder(settings.BaseUrl + "/channels")
                .Accept(HttpAccept.Json);

            requestBuilder.Headers.Add("X-Api-Key", settings.ApiKey);

            // start index
            requestBuilder.AddQueryParam("subscribed", "1")
                .AddQueryParam("first", first.ToString());

            var response = _httpClient.Execute(requestBuilder.Build());

            return Json.Deserialize<SearchResponse>(response.Content);
        }

        private SearchResponse SearchRequest(TriblerIndexerSettings settings, string query, int first, int? maxRows = null)
        {
            var requestBuilder = new HttpRequestBuilder(HttpUri.CombinePath(settings.BaseUrl, "search"))
                .Accept(HttpAccept.Json).AddQueryParam("txt_filter", query)
                .AddQueryParam("metadata_type", "torrent")
                .AddQueryParam("first", first.ToString());

            if (maxRows.HasValue)
            {
                requestBuilder.AddQueryParam("last", maxRows.ToString());
            }

            requestBuilder.Headers.Add("X-Api-Key", settings.ApiKey);

            var response = _httpClient.Execute(requestBuilder.Build());

            return Json.Deserialize<SearchResponse>(response.Content);
        }

        public static IEnumerable<TorrentMetadata> VisitPage(TriblerIndexerSettings settings, Func<int, SearchResponse> visitor)
        {
            var first = 1;

            SearchResponse lookupResponse = null;

            do
            {
                lookupResponse = visitor.Invoke(first);

                foreach (var item in lookupResponse.Results)
                {
                    yield return item;
                }

                if (lookupResponse.Last.HasValue)
                {
                    first = lookupResponse.Last.Value + 1;
                }
                else
                {
                    break;
                }
            }
            while (lookupResponse.Results.Any());
        }

        public IEnumerable<TorrentMetadata> Channel(TriblerIndexerSettings settings, string publicKey, string channelId)
        {
            return VisitPage(settings, (first) => this.GetChannel(settings, publicKey, channelId, first));
        }

        public IEnumerable<TriblerChannelSubscription> ChannelsSubscribed(TriblerIndexerSettings settings)
        {
            return VisitPage(settings, (first) => this.GetSubscribedChannels(settings, first)).Select((item) => new TriblerChannelSubscription() { PublicKey = item.PublicKey, ChannelId = item.Id.ToString() });
        }

        public IEnumerable<TorrentMetadata> Search(TriblerIndexerSettings settings, string query, int? maxRows = null)
        {
            return VisitPage(settings, (first) => this.SearchRequest(settings, query, first, maxRows));
        }

        public RemoteSearchResponse StartRemoteQuery(TriblerIndexerSettings settings, string query)
        {
            var requestBuilder = new HttpRequestBuilder(HttpUri.CombinePath(settings.BaseUrl, "remote_query"))
                .Accept(HttpAccept.Json)
                .AddQueryParam("txt_filter", query)
                .AddQueryParam("metadata_type", "torrent");

            requestBuilder.Method = HttpMethod.Put;
            requestBuilder.Headers.Add("X-Api-Key", settings.ApiKey);

            var response = _httpClient.Execute(requestBuilder.Build());

            return Json.Deserialize<RemoteSearchResponse>(response.Content);
        }
    }

    public class TriblerChannelSubscription : IEquatable<TriblerChannelSubscription>
    {
        public string PublicKey { get; set; }
        public string ChannelId { get; set; }

        public static TriblerChannelSubscription Parse(string channelSubscription)
        {
            if (channelSubscription == null || channelSubscription.Length == 0)
            {
                throw new ArgumentNullException("channelSubscription", "Channel subscription can not be null");
            }

            // a subscription is a publickey/channelid
            var strSplit = channelSubscription.Split('/');

            if (strSplit.Length != 2)
            {
                throw new ArgumentException("channelSubscription should contain a single /");
            }

            var triblerChannelSubscription = new TriblerChannelSubscription();

            var publicKey = BigInteger.Zero;

            // try to parse public key
            BigInteger.Parse(strSplit[0], System.Globalization.NumberStyles.AllowHexSpecifier);
            triblerChannelSubscription.PublicKey = strSplit[0];

            // try to parse channel id
            BigInteger.Parse(strSplit[1]);
            triblerChannelSubscription.ChannelId = strSplit[1];

            return triblerChannelSubscription;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TriblerChannelSubscription);
        }

        public bool Equals(TriblerChannelSubscription other)
        {
            return other != null &&
                   PublicKey == other.PublicKey &&
                   ChannelId == other.ChannelId;
        }

        public override int GetHashCode()
        {
            var hashCode = -440550112;
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(PublicKey);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(ChannelId);
            return hashCode;
        }

        public static bool operator ==(TriblerChannelSubscription left, TriblerChannelSubscription right)
        {
            return EqualityComparer<TriblerChannelSubscription>.Default.Equals(left, right);
        }

        public static bool operator !=(TriblerChannelSubscription left, TriblerChannelSubscription right)
        {
            return !(left == right);
        }
    }
}
