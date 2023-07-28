using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers.Tribler;

namespace NzbDrone.Core.Test.IndexerTests.TriblerTests
{
    [TestFixture]
    public class TriblerIndexerRequestGeneratorFixture
    {
        [Test]
        public void ParseSimpleQuery()
        {
            TriblerIndexerRequestGenerator.SanitizeQuery("hello world").Should().Be("hello world");
            TriblerIndexerRequestGenerator.SanitizeQuery("hello :world").Should().Be("hello world");
        }
    }
}
