using System;
using System.Threading;
using System.Threading.Tasks;
using AnonymousData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minded.Extensions.Retry.Configuration;
using Minded.Extensions.Retry.Decorator;
using Minded.Framework.CQRS.Query;
using Moq;

namespace Minded.Extensions.Retry.Tests
{
    /// <summary>
    /// Unit tests for RetryQueryHandlerDecorator.
    /// Verifies retry logic, delay handling, attribute configuration, and ApplyToAllQueries option.
    /// </summary>
    [TestClass]
    public class RetryQueryHandlerDecoratorTests
    {
        [TestMethod]
        public async Task HandleAsync_ExecutesSuccessfully_WhenNoAttributePresentAndApplyToAllQueriesFalse()
        {
            var mockInnerHandler = new Mock<IQueryHandler<TestQuery, int>>();
            var mockLogger = new Mock<ILogger<RetryQueryHandlerDecorator<TestQuery, int>>>();
            IOptions<RetryOptions> options = Options.Create(new RetryOptions());
            var sut = new RetryQueryHandlerDecorator<TestQuery, int>(mockInnerHandler.Object, mockLogger.Object, options);

            var query = new TestQuery();
            var expectedResult = Any.Int();
            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var result = await sut.HandleAsync(query, CancellationToken.None);

            result.Should().Be(expectedResult);
            mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task HandleAsync_AppliesRetry_WhenNoAttributePresentButApplyToAllQueriesTrue()
        {
            var mockInnerHandler = new Mock<IQueryHandler<TestQuery, int>>();
            var mockLogger = new Mock<ILogger<RetryQueryHandlerDecorator<TestQuery, int>>>();
            IOptions<RetryOptions> options = Options.Create(new RetryOptions { ApplyToAllQueries = true });
            var sut = new RetryQueryHandlerDecorator<TestQuery, int>(mockInnerHandler.Object, mockLogger.Object, options);

            var query = new TestQuery();
            var expectedResult = Any.Int();
            var callCount = 0;

            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestQuery>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                        throw new InvalidOperationException("First attempt failed");
                    return Task.FromResult(expectedResult);
                });

            var result = await sut.HandleAsync(query, CancellationToken.None);

            result.Should().Be(expectedResult);
            mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task HandleAsync_ExecutesSuccessfully_WhenAttributePresentAndNoFailure()
        {
            var mockInnerHandler = new Mock<IQueryHandler<TestQueryWithRetry, int>>();
            var mockLogger = new Mock<ILogger<RetryQueryHandlerDecorator<TestQueryWithRetry, int>>>();
            IOptions<RetryOptions> options = Options.Create(new RetryOptions());
            var sut = new RetryQueryHandlerDecorator<TestQueryWithRetry, int>(mockInnerHandler.Object, mockLogger.Object, options);

            var query = new TestQueryWithRetry();
            var expectedResult = Any.Int();
            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestQueryWithRetry>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var result = await sut.HandleAsync(query, CancellationToken.None);

            result.Should().Be(expectedResult);
            mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestQueryWithRetry>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task HandleAsync_RetriesOnException_WhenAttributePresent()
        {
            var mockInnerHandler = new Mock<IQueryHandler<TestQueryWithRetry, int>>();
            var mockLogger = new Mock<ILogger<RetryQueryHandlerDecorator<TestQueryWithRetry, int>>>();
            IOptions<RetryOptions> options = Options.Create(new RetryOptions());
            var sut = new RetryQueryHandlerDecorator<TestQueryWithRetry, int>(mockInnerHandler.Object, mockLogger.Object, options);

            var query = new TestQueryWithRetry();
            var expectedResult = Any.Int();
            var callCount = 0;

            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestQueryWithRetry>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                        throw new InvalidOperationException("First attempt failed");
                    return Task.FromResult(expectedResult);
                });

            var result = await sut.HandleAsync(query, CancellationToken.None);

            result.Should().Be(expectedResult);
            mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestQueryWithRetry>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task HandleAsync_ThrowsException_WhenAllRetriesExhausted()
        {
            var mockInnerHandler = new Mock<IQueryHandler<TestQueryWithRetry, int>>();
            var mockLogger = new Mock<ILogger<RetryQueryHandlerDecorator<TestQueryWithRetry, int>>>();
            IOptions<RetryOptions> options = Options.Create(new RetryOptions());
            var sut = new RetryQueryHandlerDecorator<TestQueryWithRetry, int>(mockInnerHandler.Object, mockLogger.Object, options);

            var query = new TestQueryWithRetry();
            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestQueryWithRetry>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Persistent failure"));

            Func<Task<int>> act = async () => await sut.HandleAsync(query, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Persistent failure");
            mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestQueryWithRetry>(), It.IsAny<CancellationToken>()), Times.Exactly(4)); // 1 initial + 3 retries
        }

        [TestMethod]
        public async Task HandleAsync_UsesAttributeRetryCount_WhenSpecified()
        {
            var mockInnerHandler = new Mock<IQueryHandler<TestQueryWithCustomRetryCount, int>>();
            var mockLogger = new Mock<ILogger<RetryQueryHandlerDecorator<TestQueryWithCustomRetryCount, int>>>();
            IOptions<RetryOptions> options = Options.Create(new RetryOptions());
            var sut = new RetryQueryHandlerDecorator<TestQueryWithCustomRetryCount, int>(mockInnerHandler.Object, mockLogger.Object, options);

            var query = new TestQueryWithCustomRetryCount();
            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestQueryWithCustomRetryCount>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            Func<Task<int>> act = async () => await sut.HandleAsync(query, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestQueryWithCustomRetryCount>(), It.IsAny<CancellationToken>()), Times.Exactly(3)); // 1 initial + 2 retries
        }

        [TestMethod]
        public async Task HandleAsync_UsesDefaultRetryCount_WhenApplyToAllQueriesTrueAndNoAttribute()
        {
            var mockInnerHandler = new Mock<IQueryHandler<TestQuery, int>>();
            var mockLogger = new Mock<ILogger<RetryQueryHandlerDecorator<TestQuery, int>>>();
            IOptions<RetryOptions> options = Options.Create(new RetryOptions { ApplyToAllQueries = true, DefaultRetryCount = 5 });
            var sut = new RetryQueryHandlerDecorator<TestQuery, int>(mockInnerHandler.Object, mockLogger.Object, options);

            var query = new TestQuery();
            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            Func<Task<int>> act = async () => await sut.HandleAsync(query, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(6)); // 1 initial + 5 retries
        }

        [TestMethod]
        public async Task HandleAsync_AppliesDelay_WhenSpecifiedInAttribute()
        {
            var mockInnerHandler = new Mock<IQueryHandler<TestQueryWithDelay, int>>();
            var mockLogger = new Mock<ILogger<RetryQueryHandlerDecorator<TestQueryWithDelay, int>>>();
            IOptions<RetryOptions> options = Options.Create(new RetryOptions());
            var sut = new RetryQueryHandlerDecorator<TestQueryWithDelay, int>(mockInnerHandler.Object, mockLogger.Object, options);

            var query = new TestQueryWithDelay();
            var callCount = 0;
            var expectedResult = Any.Int();

            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestQueryWithDelay>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                        throw new InvalidOperationException("First attempt failed");
                    return Task.FromResult(expectedResult);
                });

            DateTime startTime = DateTime.UtcNow;
            var result = await sut.HandleAsync(query, CancellationToken.None);
            TimeSpan elapsed = DateTime.UtcNow - startTime;

            result.Should().Be(expectedResult);
            elapsed.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(90); // 100ms delay with some tolerance
        }

    }

    // Test query classes
    public class TestQuery : IQuery<int>
    {
        public Guid TraceId { get; } = Guid.NewGuid();
    }

    [RetryQuery]
    public class TestQueryWithRetry : IQuery<int>
    {
        public Guid TraceId { get; } = Guid.NewGuid();
    }

    [RetryQuery(2)]
    public class TestQueryWithCustomRetryCount : IQuery<int>
    {
        public Guid TraceId { get; } = Guid.NewGuid();
    }

    [RetryQuery(3, 100)]
    public class TestQueryWithDelay : IQuery<int>
    {
        public Guid TraceId { get; } = Guid.NewGuid();
    }
}

