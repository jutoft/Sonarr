using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Tribler;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.TriblerTests
{
    [TestFixture]
    public class TriblerIndexerFixture : CoreTest<TriblerIndexer>
    {
        // private TriblerCapabilities _caps;
        private Mock<HttpMessageHandler> _httpResponseMock;
        private string _baseUrl = "http://localhost:51234";
        private string _apiKey = "ABC123";

        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition()
                {
                    Name = "Tribler",
                    Settings = new TriblerIndexerSettings()
                        {
                            BaseUrl = _baseUrl,
                            ApiKey = _apiKey
                        }
                };
            _httpResponseMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var httpClient = new HttpClient(_httpResponseMock.Object);
        }

        [Test]
        public void fetch_single_episode()
        {
            // ARRANGE
            _httpResponseMock
               .Protected()

               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>())

               // prepare the expected response of the mocked http call
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent(ReadAllText(@"Files/Indexers/Tribler/tribler_search_page_1.json")),
               })
               .Verifiable();

            var episodeSearch = new SingleEpisodeSearchCriteria
            {
                Series = new Tv.Series { Title = "Rick and Morty" },
                SeasonNumber = 4,
                EpisodeNumber = 4,
            };

            var releases = Subject.Fetch(episodeSearch);

            releases.Should().HaveCount(50);

            releases.First().Should().BeOfType<TorrentInfo>();
            var releaseInfo = releases.First() as TorrentInfo;

            releaseInfo.Title.Should().Be("Rick.and.Morty.S04E04.Claw.and.Hoarder.Special.Ricktims.Morty.1080p.HDTV.x264-CRiMSON[TGx]");
            releaseInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            releaseInfo.DownloadUrl.Should().Be("https://hdaccess.net/download.php?torrent=11515&passkey=123456");
            releaseInfo.InfoUrl.Should().Be("https://hdaccess.net/details.php?id=11515&hit=1");
            releaseInfo.CommentUrl.Should().Be("https://hdaccess.net/details.php?id=11515&hit=1#comments");
            releaseInfo.Indexer.Should().Be(Subject.Definition.Name);
            releaseInfo.PublishDate.Should().Be(DateTime.Parse("2015/03/14 21:10:42"));
            releaseInfo.Size.Should().Be(1062444526);
            releaseInfo.TvdbId.Should().Be(null);
            releaseInfo.TvRageId.Should().Be(null);
            releaseInfo.InfoHash.Should().Be("7701253e97e3fd940dc3302f0bb7cea0db359e78");
            releaseInfo.Seeders.Should().Be(2182);
            releaseInfo.Peers.Should().Be(581);
        }
    }
}
