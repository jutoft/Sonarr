namespace NzbDrone.Core.Indexers.Tribler
{
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.5.2.0 (Newtonsoft.Json v11.0.0.0)")]
    public partial class SearchResponse
    {
        [Newtonsoft.Json.JsonProperty("results", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.ICollection<TorrentMetadata> Results { get; set; }

        /// <summary>Paged query, the first</summary>
        [Newtonsoft.Json.JsonProperty("first", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int? First { get; set; }

        /// <summary>Paged query, the last item</summary>
        [Newtonsoft.Json.JsonProperty("last", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int? Last { get; set; }

        /// <summary>What field to sort by</summary>
        [Newtonsoft.Json.JsonProperty("sort_by", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Sort_by { get; set; }

        /// <summary>Sort direction</summary>
        [Newtonsoft.Json.JsonProperty("sort_desc", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool? Sort_desc { get; set; }


    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.5.2.0 (Newtonsoft.Json v11.0.0.0)")]
    public partial class CompletionsResponse
    {
        [Newtonsoft.Json.JsonProperty("completions", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.ICollection<string> Completions { get; set; }


    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.5.2.0 (Newtonsoft.Json v11.0.0.0)")]
    public partial class RemoteSearchResponse
    {
        [Newtonsoft.Json.JsonProperty("request_uuid", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Request_uuid { get; set; }

        [Newtonsoft.Json.JsonProperty("peers", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.ICollection<string> Peers { get; set; }

    }

    // src/tribler-core/tribler_core/components/metadata_store/db/orm_bindings/channel_node.py
    public enum TriblerMetadataStatus
    {
        NEW = 0, // The entry is newly created and is not published yet. It will be committed at the next commit.
        TODELETE = 1, // The entry is marked to be removed at the next commit.
        COMMITTED = 2, // The entry is committed and seeded.
        UPDATED = 6, // One of the entry's properties was updated. It will be committed at the next commit.
        LEGACY_ENTRY = 1000, // The entry was converted from the old Tribler DB. It has no signature and should not be shared.
    }

    // src/tribler-core/tribler_core/components/metadata_store/db/serialization.py
    public enum TriblerMetadataType
    {
        Typeless = 100,
        ChannelNode = 200,
        MetadataNode = 210,
        CollectionNode = 220,
        JsonNode = 230,
        ChannelDescription = 231,
        BinaryNode = 240,
        ChannelThumbnail = 241,
        RegularTorrent = 300,
        ChannelTorrent = 400,
        Deleted = 500,
        Snippet = 600,
    }

    // below items are based on trible source: src/tribler-core/tribler_core/components/metadata_store/restapi/metadata_schema.py
    public partial class TorrentMetadata
    {
        // MetadataSchema

        [Newtonsoft.Json.JsonProperty("category", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Category { get; set; }

        [Newtonsoft.Json.JsonProperty("id", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public long Id { get; set; }

        [Newtonsoft.Json.JsonProperty("name", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Name { get; set; }

        [Newtonsoft.Json.JsonProperty("origin_id", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public long OriginId { get; set; }

        [Newtonsoft.Json.JsonProperty("progress", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double? Progress { get; set; }

        [Newtonsoft.Json.JsonProperty("public_key", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string PublicKey { get; set; }

        [Newtonsoft.Json.JsonProperty("type", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public TriblerMetadataType Type { get; set; }

        // CollectionMetadataSchema
        [Newtonsoft.Json.JsonProperty("description_flag", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool? DescriptionFlag { get; set; }

        [Newtonsoft.Json.JsonProperty("state", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string State { get; set; }

        [Newtonsoft.Json.JsonProperty("thumbnail_flag", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool? ThumbnailFlag { get; set; }

        [Newtonsoft.Json.JsonProperty("torrents", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int? Torrents { get; set; }

        [Newtonsoft.Json.JsonProperty("torrents_in_snippet", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.ICollection<TorrentMetadata> TorrentsInSnippets { get; set; }

        // TorrentMetadataSchema
        [Newtonsoft.Json.JsonProperty("infohash", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Infohash { get; set; }

        [Newtonsoft.Json.JsonProperty("num_leechers", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int? Num_leechers { get; set; }

        [Newtonsoft.Json.JsonProperty("num_seeders", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int? Num_seeders { get; set; }

        [Newtonsoft.Json.JsonProperty("last_tracker_check", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public long? Last_tracker_check { get; set; }

        [Newtonsoft.Json.JsonProperty("size", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public long? Size { get; set; }

        [Newtonsoft.Json.JsonProperty("status", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public TriblerMetadataStatus? Status { get; set; }

        [Newtonsoft.Json.JsonProperty("updated", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public long? Updated { get; set; }
        [Newtonsoft.Json.JsonProperty("created", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public long? Created { get; set; }

        // ChannelMetadataSchema
        [Newtonsoft.Json.JsonProperty("dirty", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool? IsDirty { get; set; }

        /// <summary>
        /// If tribler is subscribed to the channel
        /// </summary>
        [Newtonsoft.Json.JsonProperty("subscribed", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool? Subscribed { get; set; }

        [Newtonsoft.Json.JsonProperty("votes", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double? Votes { get; set; }
    }
}
