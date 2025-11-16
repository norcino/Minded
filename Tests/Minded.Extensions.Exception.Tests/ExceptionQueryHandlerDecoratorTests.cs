using AnonymousData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Exception.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Query;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Minded.Extensions.Exception.Tests
{
    /// <summary>
    /// Unit tests for ExceptionQueryHandlerDecorator.
    /// Tests exception handling, logging, and delegation to inner handler.
    /// </summary>
    [TestClass]
    public class ExceptionQueryHandlerDecoratorTests
    {
        private Mock<IQueryHandler<TestQuery, int>> _mockInnerHandler;
        private Mock<ILogger<ExceptionQueryHandlerDecorator<TestQuery, int>>> _mockLogger;
        private ExceptionQueryHandlerDecorator<TestQuery, int> _sut;

        [TestInitialize]
        public void Setup()
        {
            _mockInnerHandler = new Mock<IQueryHandler<TestQuery, int>>();
            _mockLogger = new Mock<ILogger<ExceptionQueryHandlerDecorator<TestQuery, int>>>();
            _sut = new ExceptionQueryHandlerDecorator<TestQuery, int>(_mockInnerHandler.Object, _mockLogger.Object);
        }

        /// <summary>
        /// Tests that HandleAsync delegates to inner handler when no exception occurs.
        /// Verifies successful result is returned.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenNoException_DelegatesToInnerHandler()
        {
            var query = new TestQuery();
            var expectedResult = Any.Int();
            _mockInnerHandler.Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var result = await _sut.HandleAsync(query);

            result.Should().Be(expectedResult);
            _mockInnerHandler.Verify(h => h.HandleAsync(query, It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests that HandleAsync wraps exceptions in QueryHandlerException.
        /// Verifies exception is logged and wrapped correctly.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenExceptionThrown_WrapsInQueryHandlerException()
        {
            var query = new TestQuery();
            var innerException = new InvalidOperationException(Any.String());
            _mockInnerHandler.Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
                .ThrowsAsync(innerException);

            Func<Task> act = async () => await _sut.HandleAsync(query);

            await act.Should().ThrowAsync<QueryHandlerException<TestQuery, int>>()
                .Where(ex => ex.InnerException == innerException);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    innerException,
                    It.IsAny<Func<It.IsAnyType, System.Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that OperationCanceledException is logged as information and re-thrown.
        /// Verifies cancellation is not treated as an error.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenOperationCanceled_LogsInformationAndRethrows()
        {
            var query = new TestQuery();
            var cancelledException = new OperationCanceledException();
            _mockInnerHandler.Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
                .ThrowsAsync(cancelledException);

            Func<Task> act = async () => await _sut.HandleAsync(query);

            await act.Should().ThrowAsync<OperationCanceledException>();
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("was cancelled")),
                    null,
                    It.IsAny<Func<It.IsAnyType, System.Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that CancellationToken is passed through to inner handler.
        /// Verifies cancellation token propagation.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_PassesCancellationTokenToInnerHandler()
        {
            var query = new TestQuery();
            var cancellationToken = new CancellationToken();
            var expectedResult = Any.Int();
            _mockInnerHandler.Setup(h => h.HandleAsync(query, cancellationToken))
                .ReturnsAsync(expectedResult);

            await _sut.HandleAsync(query, cancellationToken);

            _mockInnerHandler.Verify(h => h.HandleAsync(query, cancellationToken), Times.Once);
        }

        /// <summary>
        /// Tests that QueryHandlerException contains the query instance.
        /// Verifies exception includes query for debugging.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenExceptionThrown_ExceptionContainsQuery()
        {
            var query = new TestQuery();
            var innerException = new InvalidOperationException(Any.String());
            _mockInnerHandler.Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
                .ThrowsAsync(innerException);

            try
            {
                await _sut.HandleAsync(query);
                Assert.Fail("Expected exception was not thrown");
            }
            catch (QueryHandlerException<TestQuery, int> ex)
            {
                ex.Query.Should().Be(query);
                ex.InnerException.Should().Be(innerException);
            }
        }
    }

    public class TestQuery : IQuery<int>
    {
        public Guid TraceId { get; set; } = Any.Guid();
    }
}

