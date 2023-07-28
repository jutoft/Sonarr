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
        }

        [Test]
        public void ParseWithColon()
        {
            TriblerIndexerRequestGenerator.SanitizeQuery("hello :world").Should().Be("hello world");
        }

        [Test]
        public void ParseWithWierdChars()
        {
            TriblerIndexerRequestGenerator.SanitizeQuery("hello :world #123").Should().Be("hello world 123");
        }
    }
}
