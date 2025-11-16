using AnonymousData;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Caching.Decorator;
using Minded.Extensions.Caching.Memory.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Query;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Minded.Extensions.Caching.Memory.Tests
{
    /// <summary>
    /// Unit tests for MemoryCacheQueryHandlerDecorator.
    /// Tests caching behavior, cache key generation, expiration, and error handling.
    /// </summary>
    [TestClass]
    public class MemoryCacheQueryHandlerDecoratorTests
    {
        private Mock<IQueryHandler<TestCachedQuery, IQueryResponse<int>>> _mockInnerHandler;
        private Mock<IMemoryCache> _mockCache;
        private Mock<IGlobalCacheKeyPrefixProvider> _mockPrefixProvider;
        private MemoryCacheQueryHandlerDecorator<TestCachedQuery, IQueryResponse<int>> _sut;

        [TestInitialize]
        public void Setup()
        {
            _mockInnerHandler = new Mock<IQueryHandler<TestCachedQuery, IQueryResponse<int>>>();
            _mockCache = new Mock<IMemoryCache>();
            _mockPrefixProvider = new Mock<IGlobalCacheKeyPrefixProvider>();
            _mockPrefixProvider.Setup(p => p.GetGlobalCacheKeyPrefix()).Returns(Any.String());
            _sut = new MemoryCacheQueryHandlerDecorator<TestCachedQuery, IQueryResponse<int>>(
                _mockInnerHandler.Object,
                _mockCache.Object,
                _mockPrefixProvider.Object);
        }

        /// <summary>
        /// Tests that HandleAsync bypasses caching when query has no MemoryCacheAttribute.
        /// Verifies cache is not accessed for non-cached queries.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenQueryNotCached_BypassesCaching()
        {
            var query = new TestNonCachedQuery();
            var expectedResult = new QueryResponse<string>(Any.String());
            var mockHandler = new Mock<IQueryHandler<TestNonCachedQuery, IQueryResponse<string>>>();
            var mockCache = new Mock<IMemoryCache>();
            var mockPrefixProvider = new Mock<IGlobalCacheKeyPrefixProvider>();
            var sut = new MemoryCacheQueryHandlerDecorator<TestNonCachedQuery, IQueryResponse<string>>(
                mockHandler.Object,
                mockCache.Object,
                mockPrefixProvider.Object);
            mockHandler.Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var result = await sut.HandleAsync(query);

            result.Should().Be(expectedResult);
            mockCache.Verify(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny), Times.Never);
            mockHandler.Verify(h => h.HandleAsync(query, It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests that HandleAsync throws InvalidOperationException when query doesn't implement IGenerateCacheKey.
        /// Verifies cache key generation interface is required.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenQueryDoesNotImplementIGenerateCacheKey_ThrowsInvalidOperationException()
        {
            var query = new TestCachedQueryWithoutCacheKey();
            var mockHandler = new Mock<IQueryHandler<TestCachedQueryWithoutCacheKey, IQueryResponse<int>>>();
            var mockCache = new Mock<IMemoryCache>();
            var mockPrefixProvider = new Mock<IGlobalCacheKeyPrefixProvider>();
            var sut = new MemoryCacheQueryHandlerDecorator<TestCachedQueryWithoutCacheKey, IQueryResponse<int>>(
                mockHandler.Object,
                mockCache.Object,
                mockPrefixProvider.Object);

            Func<Task> act = async () => await sut.HandleAsync(query);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*IGenerateCacheKey*");
        }

        /// <summary>
        /// Tests that HandleAsync returns cached result when cache hit occurs.
        /// Verifies inner handler is not called on cache hit.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenCacheHit_ReturnsCachedResult()
        {
            var query = new TestCachedQuery();
            var cachedResult = new QueryResponse<int>(Any.Int());
            object cacheValue = cachedResult;
            _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue))
                .Returns(true);

            var result = await _sut.HandleAsync(query);

            result.Should().Be(cachedResult);
            _mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestCachedQuery>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Tests that HandleAsync calls inner handler and caches result on cache miss.
        /// Verifies result is stored in cache after execution.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenCacheMiss_CallsInnerHandlerAndCachesResult()
        {
            var query = new TestCachedQuery();
            var handlerResult = new QueryResponse<int>(Any.Int());
            object cacheValue = null;
            _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue))
                .Returns(false);
            _mockInnerHandler.Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
                .ReturnsAsync(handlerResult);
            var mockCacheEntry = new Mock<ICacheEntry>();
            _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>()))
                .Returns(mockCacheEntry.Object);

            var result = await _sut.HandleAsync(query);

            result.Should().Be(handlerResult);
            _mockInnerHandler.Verify(h => h.HandleAsync(query, It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(c => c.CreateEntry(It.IsAny<object>()), Times.Once);
        }

        /// <summary>
        /// Tests that HandleAsync does not cache unsuccessful IQueryResponse results.
        /// Verifies only successful responses are cached.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenResultUnsuccessful_DoesNotCache()
        {
            var query = new TestCachedQuery();
            var handlerResult = new QueryResponse<int>(Any.Int()) { Successful = false };
            object cacheValue = null;
            _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue))
                .Returns(false);
            _mockInnerHandler.Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
                .ReturnsAsync(handlerResult);

            var result = await _sut.HandleAsync(query);

            result.Should().Be(handlerResult);
            _mockCache.Verify(c => c.CreateEntry(It.IsAny<object>()), Times.Never);
        }

        /// <summary>
        /// Tests that HandleAsync generates cache key using global prefix and query cache key.
        /// Verifies cache key format is correct.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_GeneratesCacheKeyWithGlobalPrefix()
        {
            var globalPrefix = Any.String();
            var queryCacheKey = Any.String();
            var query = new TestCachedQuery { CacheKey = queryCacheKey };
            var handlerResult = new QueryResponse<int>(Any.Int());
            object cacheValue = null;
            _mockPrefixProvider.Setup(p => p.GetGlobalCacheKeyPrefix()).Returns(globalPrefix);
            _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue))
                .Returns(false);
            _mockInnerHandler.Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
                .ReturnsAsync(handlerResult);
            var mockCacheEntry = new Mock<ICacheEntry>();
            _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>()))
                .Returns(mockCacheEntry.Object);

            await _sut.HandleAsync(query);

            _mockCache.Verify(c => c.TryGetValue($"{globalPrefix}-{queryCacheKey}", out cacheValue), Times.Once);
        }
    }

    [MemoryCache]
    public class TestCachedQuery : IQuery<IQueryResponse<int>>, IGenerateCacheKey
    {
        public Guid TraceId { get; set; } = Any.Guid();
        public string CacheKey { get; set; } = Any.String();
        public string GetCacheKey() => CacheKey;
    }

    public class TestNonCachedQuery : IQuery<IQueryResponse<string>>
    {
        public Guid TraceId { get; set; } = Any.Guid();
    }

    [MemoryCache(failOnError: true)]
    public class TestCachedQueryWithoutCacheKey : IQuery<IQueryResponse<int>>
    {
        public Guid TraceId { get; set; } = Any.Guid();
    }
}

