using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Http.CloudFlare;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;
using Tribler.Api;

namespace NzbDrone.Core.Indexers.Tribler
{
    public class TriblerIndexer : IndexerBase<TriblerIndexerSettings>
    {

        private System.Net.Http.HttpClient _httpClient;

        private readonly Lazy<ITriblerSearchRequestGenerator> searchRequestGenerator;

        private readonly Lazy<ITriblerApiClient> _triblerApiClient;

        public override string Name => "Tribler";

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        // TODO: Consider using channels as rss feed.
        public override bool SupportsRss => false;
        public override bool SupportsSearch => true;

        public TriblerIndexer(IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, Logger logger)
            : base(indexerStatusService, configService, parsingService, logger)
        {

            this._triblerApiClient = new Lazy<ITriblerApiClient>(CreateClient);
            this.searchRequestGenerator = new Lazy<ITriblerSearchRequestGenerator>(CreateRequestGenerator);
        }

        protected ITriblerApiClient CreateClient()
        {
            _httpClient = new System.Net.Http.HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", Settings.ApiKey);
            return new TriblerApiClient(Settings.BaseUrl, _httpClient);
        }

        protected ITriblerSearchRequestGenerator CreateRequestGenerator()
        {
            return new TriblerRequestGenerator(_triblerApiClient.Value);
        }

        public override IList<ReleaseInfo> Fetch(SingleEpisodeSearchCriteria searchCriteria)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            foreach (var episode in searchCriteria.Episodes)
            {
                IList<ReleaseInfo> results = searchRequestGenerator.Value.Search(string.Format("{0} S{1:00}E{2:00}", searchCriteria.Series.Title, episode.SeasonNumber, episode.EpisodeNumber));

                releases.AddRange(results);
            }

            return CleanupReleases(releases);
        }

        public override IList<ReleaseInfo> Fetch(SeasonSearchCriteria searchCriteria)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            foreach (var episode in searchCriteria.Episodes)
            {
                IList<ReleaseInfo> results = searchRequestGenerator.Value.Search(string.Format("{0} S{1:00}E", searchCriteria.Series.Title, episode.SeasonNumber));

                releases.AddRange(results);
            }

            foreach (var seasonNumber in searchCriteria.Episodes.Select(v => v.SeasonNumber).Distinct())
            {
                IList<ReleaseInfo> results = searchRequestGenerator.Value.Search(string.Format("{0} Season {1}", searchCriteria.Series.Title, seasonNumber));

                releases.AddRange(results);
            }

            return CleanupReleases(releases);
        }

        public override IList<ReleaseInfo> Fetch(DailyEpisodeSearchCriteria searchCriteria)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            IList<ReleaseInfo> results = searchRequestGenerator.Value.Search(string.Format("{0} {1:yyyy}.{1:MM}.{1:dd}", searchCriteria.Series.Title, searchCriteria.AirDate));

                releases.AddRange(results);

            foreach (var episode in searchCriteria.Episodes)
            {
                results = searchRequestGenerator.Value.Search(string.Format("{0} S{1:00}E{2:00}", searchCriteria.Series.Title, episode.SeasonNumber, episode.EpisodeNumber));

                releases.AddRange(results);
            }

            return CleanupReleases(releases);
        }

        public override IList<ReleaseInfo> Fetch(DailySeasonSearchCriteria searchCriteria)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            IList<ReleaseInfo> results = searchRequestGenerator.Value.Search(string.Format("{0} {1}", searchCriteria.Series.Title, searchCriteria.Year));

            releases.AddRange(results);

            foreach (var episode in searchCriteria.Episodes)
            {
                results = searchRequestGenerator.Value.Search(string.Format("{0} S{1:00}E{2:00}", searchCriteria.Series.Title, episode.SeasonNumber, episode.EpisodeNumber));

                releases.AddRange(results);
            }

            return CleanupReleases(releases);
        }

        public override IList<ReleaseInfo> Fetch(AnimeEpisodeSearchCriteria searchCriteria)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            foreach (var episode in searchCriteria.Episodes)
            {
                IList<ReleaseInfo> results = searchRequestGenerator.Value.Search(string.Format("{0} S{1:00}E{2:00}", searchCriteria.Series.Title, episode.SeasonNumber, episode.EpisodeNumber));

                releases.AddRange(results);
            }

            foreach (var seasonNumber in searchCriteria.Episodes.Select(v => v.SeasonNumber).Distinct())
            {
                IList<ReleaseInfo> results = searchRequestGenerator.Value.Search(string.Format("{0} Season {1}", searchCriteria.Series.Title, seasonNumber));

                releases.AddRange(results);
            }

            return CleanupReleases(releases);
        }

        public override IList<ReleaseInfo> Fetch(SpecialEpisodeSearchCriteria searchCriteria)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var episodeQueryTitle = searchCriteria.EpisodeQueryTitles.Where(e => !string.IsNullOrWhiteSpace(e))
                               .Select(e => SearchCriteriaBase.GetCleanSceneTitle(e))
                               .ToArray();

            foreach (var queryTitle in episodeQueryTitle)
            {
                var query = queryTitle.Replace('+', ' ');
                query = System.Web.HttpUtility.UrlEncode(query);

                IList<ReleaseInfo> results = searchRequestGenerator.Value.Search(query);

                releases.AddRange(results);
            }


            return CleanupReleases(releases);
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            NzbDroneValidationResult nzbDroneValidationResult = Settings.Validate();
            failures.AddRange(nzbDroneValidationResult.Errors);

            try
            {
                IList<ReleaseInfo> searchResponse = searchRequestGenerator.Value.Search("Ubuntu");

                if (searchResponse == null || searchResponse.Count == 0)
                {
                    failures.Add(new ValidationFailure("search", "returned no results"));
                }

            }
            catch (Exception ex)
            {
                failures.Add(new ValidationFailure("search", "connection errors or unothorized access"));
                _logger.Error(ex);
            }
        }

        public override IList<ReleaseInfo> FetchRecent()
        {
            return new List<ReleaseInfo>();
        }
    }
}