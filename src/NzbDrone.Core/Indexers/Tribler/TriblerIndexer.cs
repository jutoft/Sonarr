using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Tribler
{
    public class TriblerIndexer : IndexerBase<TriblerIndexerSettings>
    {
        private ITriblerIndexerRequestGenerator _requestGenerator;

        public override string Name => "Tribler";

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;

        public TriblerIndexer(ITriblerIndexerRequestGenerator requestGenerator, IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, Logger logger)
            : base(indexerStatusService, configService, parsingService, logger)
        {
            this._requestGenerator = requestGenerator;
        }

        public override IList<ReleaseInfo> FetchRecent()
        {
            return _requestGenerator.FetchRecent(Definition, Settings);
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            var nzbDroneValidationResult = Settings.Validate();
            failures.AddRange(nzbDroneValidationResult.Errors);

            try
            {
                var releaseInfos = new List<ReleaseInfo>();
                releaseInfos.AddRange(_requestGenerator.FetchSubscribed(Definition, Settings));
                releaseInfos.AddRange(_requestGenerator.Search(Definition, Settings, "ubuntu", 1));

                if (releaseInfos == null || releaseInfos.Count == 0)
                {
                    failures.Add(new ValidationFailure("rss", "returned no results"));
                }
            }
            catch (ApiKeyException ex)
            {
                _logger.Warn("Indexer returned result for RSS URL, API Key appears to be invalid: " + ex.Message);

                failures.Add(new ValidationFailure("ApiKey", "Invalid API Key"));
            }
            catch (IndexerException ex)
            {
                _logger.Warn(ex, "Unable to connect to indexer");

                failures.Add(new ValidationFailure(string.Empty, "Unable to connect to indexer. " + ex.Message));
            }
            catch (HttpException ex)
            {
                switch (ex.Response.StatusCode)
                {
                    case System.Net.HttpStatusCode.Unauthorized:
                        _logger.Warn("Response from indexer: API Key appears to be invalid: " + ex.Message);
                        failures.Add(new ValidationFailure("ApiKey", "Invalid API Key"));
                        break;

                    default:
                        _logger.Warn(ex, "Error response from indexer");
                        failures.Add(new ValidationFailure(string.Empty, "Unable to connect to indexer. " + ex.Message));
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to connect to indexer");

                failures.Add(new ValidationFailure(string.Empty, "Unable to connect to indexer, check the log for more details"));
            }
        }

        public override IList<ReleaseInfo> Fetch(SeasonSearchCriteria searchCriteria)
        {
            var releaseInfo = new List<ReleaseInfo>();

            foreach (var seasonNumber in searchCriteria.Episodes.Select(e => e.SeasonNumber).Distinct())
            {
                var query = string.Format("{0} S{1:00}E", searchCriteria.Series.Title, seasonNumber);
                releaseInfo.AddRange(_requestGenerator.Search(Definition, Settings, query));

                query = string.Format("{0} Season {1}", searchCriteria.Series.Title, seasonNumber);
                releaseInfo.AddRange(_requestGenerator.Search(Definition, Settings, query));
            }

            return releaseInfo;
        }

        public override IList<ReleaseInfo> Fetch(SingleEpisodeSearchCriteria searchCriteria)
        {
            var releaseInfo = new List<ReleaseInfo>();

            var query = string.Format("{0} S{1:00}E{2:00}", searchCriteria.Series.Title, searchCriteria.SeasonNumber, searchCriteria.EpisodeNumber);

            releaseInfo.AddRange(_requestGenerator.Search(Definition, Settings, query));

            return releaseInfo;
        }

        public override IList<ReleaseInfo> Fetch(DailyEpisodeSearchCriteria searchCriteria)
        {
            var query = string.Format("{0} {1:yyyy}.{1:MM}.{1:dd}", searchCriteria.Series.Title, searchCriteria.AirDate);
            return _requestGenerator.Search(Definition, Settings, query);
        }

        public override IList<ReleaseInfo> Fetch(DailySeasonSearchCriteria searchCriteria)
        {
            var query = string.Format("{0} {1}", searchCriteria.Series.Title, searchCriteria.Year);
            return _requestGenerator.Search(Definition, Settings, query);
        }

        public override IList<ReleaseInfo> Fetch(AnimeEpisodeSearchCriteria searchCriteria)
        {
            var releaseInfo = new List<ReleaseInfo>();

            foreach (var episode in searchCriteria.Episodes)
            {
                var query = string.Format("{0} S{1:00}E{2:00}", searchCriteria.Series.Title, episode.SeasonNumber, episode.EpisodeNumber);
                releaseInfo.AddRange(_requestGenerator.Search(Definition, Settings, query));
            }

            return releaseInfo;
        }

        public override IList<ReleaseInfo> Fetch(SpecialEpisodeSearchCriteria searchCriteria)
        {
            var releaseInfo = new List<ReleaseInfo>();

            // not sure if this is the correct way to handle special episodes, it's mostly copy-paste.
            var episodeQueryTitle = searchCriteria.EpisodeQueryTitles.Where(e => !string.IsNullOrWhiteSpace(e))
                   .Select(e => SearchCriteriaBase.GetCleanSceneTitle(e))
                   .ToArray();

            foreach (var queryTitle in episodeQueryTitle)
            {
                var query = queryTitle.Replace('+', ' ');
                query = System.Web.HttpUtility.UrlEncode(query);

                releaseInfo.AddRange(_requestGenerator.Search(Definition, Settings, query));
            }

            return releaseInfo;
        }

        public override HttpRequest GetDownloadRequest(string link)
        {
            return null;
        }
    }
}
