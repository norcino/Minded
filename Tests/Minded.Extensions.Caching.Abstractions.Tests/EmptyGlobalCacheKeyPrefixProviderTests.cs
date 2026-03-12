using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Caching.Abstractions.Decorator;

namespace Minded.Extensions.Caching.Abstractions.Tests
{
    /// <summary>
    /// Unit tests for EmptyGlobalCacheKeyPrefixProvider class.
    /// Tests that the provider returns an empty string as the global cache key prefix.
    /// </summary>
    [TestClass]
    public class EmptyGlobalCacheKeyPrefixProviderTests
    {
        /// <summary>
        /// Tests that GetGlobalCacheKeyPrefix returns an empty string.
        /// </summary>
        [TestMethod]
        public void GetGlobalCacheKeyPrefix_ReturnsEmptyString()
        {
            var provider = new EmptyGlobalCacheKeyPrefixProvider();

            var result = provider.GetGlobalCacheKeyPrefix();

            result.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that GetGlobalCacheKeyPrefix consistently returns an empty string on multiple calls.
        /// </summary>
        [TestMethod]
        public void GetGlobalCacheKeyPrefix_MultipleCalls_ReturnsEmptyString()
        {
            var provider = new EmptyGlobalCacheKeyPrefixProvider();

            var result1 = provider.GetGlobalCacheKeyPrefix();
            var result2 = provider.GetGlobalCacheKeyPrefix();

            result1.Should().BeEmpty();
            result2.Should().BeEmpty();
            result1.Should().Be(result2);
        }
    }
}

