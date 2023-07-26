﻿using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using System.Collections.Generic;
using System.Net;
using Tribler.Api;

namespace NzbDrone.Core.Download.Clients.Tribler
{
    using TriblerApi = global::Tribler.Api;

    public interface ITriblerDownloadClientProxy
    {
        ICollection<TriblerApi.Download> GetDownloads(TriblerDownloadSettings settings);

        ICollection<File> GetDownloadFiles(TriblerDownloadSettings settings, TriblerApi.Download downloadItem);

        GetTriblerSettingsResponse GetConfig(TriblerDownloadSettings settings);

        void RemoveDownload(TriblerDownloadSettings settings, DownloadClientItem item, bool deleteData);

        string AddFromMagnetLink(TriblerDownloadSettings settings, AddDownloadRequest downloadRequest);
    }

    public class TriblerDownloadClientProxy : ITriblerDownloadClientProxy
    {
        protected readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public TriblerDownloadClientProxy(IHttpClient httpClient, Logger logger)
        {
            this._httpClient = httpClient;
            this._logger = logger;
        }

        private HttpRequestBuilder getRequestBuilder(TriblerDownloadSettings settings, string relativePath = null)
        {
            var requestBuilder = new HttpRequestBuilder(GetBaseUrl(settings, relativePath))
                .Accept(HttpAccept.Json);

            requestBuilder.Headers.Add("X-Api-Key", settings.ApiKey);

            requestBuilder.LogResponseContent = true;

            return requestBuilder;
        }

        public string GetBaseUrl(TriblerDownloadSettings settings, string relativePath = null)
        {
            var baseUrl = HttpRequestBuilder.BuildBaseUrl(settings.UseSsl, settings.Host, settings.Port, settings.UrlBase);
            baseUrl = HttpUri.CombinePath(baseUrl, relativePath);

            return baseUrl;
        }

        private T ProcessRequest<T>(HttpRequestBuilder requestBuilder) where T : new()
        {
            return ProcessRequest<T>(requestBuilder.Build());
        }

        private T ProcessRequest<T>(HttpRequest requestBuilder) where T : new()
        {
            var httpRequest = requestBuilder;

            HttpResponse response;

            _logger.Debug("Url: {0}", httpRequest.Url);

            try
            {
                response = _httpClient.Execute(httpRequest);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new DownloadClientAuthenticationException("Unauthorized - AuthToken is invalid", ex);
                }

                throw new DownloadClientUnavailableException("Unable to connect to Tribler. Status Code: {0}", ex.Response.StatusCode, ex);
            }

            return Json.Deserialize<T>(response.Content);
        }

        public GetTriblerSettingsResponse GetConfig(TriblerDownloadSettings settings)
        {
            var configRequest = getRequestBuilder(settings, "settings");
            return ProcessRequest<GetTriblerSettingsResponse>(configRequest);
        }

        public ICollection<File> GetDownloadFiles(TriblerDownloadSettings settings, TriblerApi.Download downloadItem)
        {
            var filesRequest = getRequestBuilder(settings, "downloads/" + downloadItem.Infohash + "/files");
            return ProcessRequest<GetFilesResponse>(filesRequest).Files;
        }

        public ICollection<TriblerApi.Download> GetDownloads(TriblerDownloadSettings settings)
        {
            var downloadRequest = getRequestBuilder(settings, "downloads");
            var downloads = ProcessRequest<DownloadsResponse>(downloadRequest);
            return downloads.Downloads;
        }

        public void RemoveDownload(TriblerDownloadSettings settings, DownloadClientItem item, bool deleteData)
        {
            var deleteDownloadRequestObject = new RemoveDownloadRequest
            {
                Remove_data = deleteData
            };

            var deleteRequestBuilder = getRequestBuilder(settings, "downloads/" + item.DownloadId.ToLower());
            deleteRequestBuilder.Method = HttpMethod.DELETE;

            // manually set content of delete request.
            var deleteRequest = deleteRequestBuilder.Build();
            deleteRequest.SetContent(Json.ToJson(deleteDownloadRequestObject));

            ProcessRequest<DeleteDownloadResponse>(deleteRequest);
        }

        public string AddFromMagnetLink(TriblerDownloadSettings settings, AddDownloadRequest downloadRequest)
        {
            // run hash through InfoHash class to ensure the correct casing.
            var addDownloadRequestBuilder = getRequestBuilder(settings, "downloads");
            addDownloadRequestBuilder.Method = HttpMethod.PUT;

            // manually set content of delete request.
            var addDownloadRequest = addDownloadRequestBuilder.Build();
            addDownloadRequest.SetContent(Json.ToJson(downloadRequest));

            string infoHashAsString = ProcessRequest<AddDownloadResponse>(addDownloadRequest).Infohash;

            var infoHash = MonoTorrent.InfoHash.FromHex(infoHashAsString);
            return infoHash.ToHex();
        }
    }
}
