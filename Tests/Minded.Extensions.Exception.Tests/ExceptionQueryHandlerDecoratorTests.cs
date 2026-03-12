using AnonymousData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Exception.Configuration;
using Minded.Extensions.Exception.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Abstractions.Sanitization;
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
        private Mock<ILoggingSanitizerPipeline> _mockSanitizerPipeline;
        private IOptions<ExceptionOptions> _options;
        private ExceptionQueryHandlerDecorator<TestQuery, int> _sut;

        [TestInitialize]
        public void Setup()
        {
            _mockInnerHandler = new Mock<IQueryHandler<TestQuery, int>>();
            _mockLogger = new Mock<ILogger<ExceptionQueryHandlerDecorator<TestQuery, int>>>();
            _mockSanitizerPipeline = new Mock<ILoggingSanitizerPipeline>();
            _options = Options.Create(new ExceptionOptions());

            // Setup the pipeline to return a simple dictionary by default
            _mockSanitizerPipeline
                .Setup(p => p.Sanitize(It.IsAny<object>()))
                .Returns(new System.Collections.Generic.Dictionary<string, object>());

            _sut = new ExceptionQueryHandlerDecorator<TestQuery, int>(
                _mockInnerHandler.Object,
                _mockLogger.Object,
                _mockSanitizerPipeline.Object,
                _options);
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

        /// <summary>
        /// Tests that query is serialized when Serialize option is true (default).
        /// Verifies serialized JSON is included in exception message.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenSerializeIsTrue_IncludesSerializedQueryInException()
        {
            var query = new TestQuery();
            var innerException = new InvalidOperationException(Any.String());
            _mockInnerHandler.Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
                .ThrowsAsync(innerException);

            var sanitizedData = new System.Collections.Generic.Dictionary<string, object>
            {
                { "TraceId", query.TraceId.ToString() }
            };
            _mockSanitizerPipeline.Setup(p => p.Sanitize(query)).Returns(sanitizedData);

            try
            {
                await _sut.HandleAsync(query);
                Assert.Fail("Expected exception was not thrown");
            }
            catch (QueryHandlerException<TestQuery, int> ex)
            {
                ex.Message.Should().Contain("QueryHandlerException:");
                ex.Message.Should().Contain(query.TraceId.ToString());
                ex.Message.Should().NotContain("serialization disabled");
            }

            _mockSanitizerPipeline.Verify(p => p.Sanitize(query), Times.Once);
        }

        /// <summary>
        /// Tests that query is NOT serialized when Serialize option is false.
        /// Verifies only type name is included in exception message.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenSerializeIsFalse_IncludesOnlyTypeNameInException()
        {
            // Create decorator with serialization disabled
            var options = Options.Create(new ExceptionOptions { Serialize = false });
            var sut = new ExceptionQueryHandlerDecorator<TestQuery, int>(
                _mockInnerHandler.Object,
                _mockLogger.Object,
                _mockSanitizerPipeline.Object,
                options);

            var query = new TestQuery();
            var innerException = new InvalidOperationException(Any.String());
            _mockInnerHandler.Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
                .ThrowsAsync(innerException);

            try
            {
                await sut.HandleAsync(query);
                Assert.Fail("Expected exception was not thrown");
            }
            catch (QueryHandlerException<TestQuery, int> ex)
            {
                ex.Message.Should().Contain("QueryHandlerException:");
                ex.Message.Should().Contain("Type: TestQuery");
                ex.Message.Should().Contain("serialization disabled");
            }

            // Verify sanitizer was NOT called
            _mockSanitizerPipeline.Verify(p => p.Sanitize(It.IsAny<object>()), Times.Never);
        }

        /// <summary>
        /// Tests that SerializeProvider takes precedence over Serialize property.
        /// Verifies dynamic provider is used when set.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenSerializeProviderIsSet_UsesProviderValue()
        {
            bool shouldSerialize = false;
            var options = Options.Create(new ExceptionOptions
            {
                Serialize = true, // Static value is true
                SerializeProvider = () => shouldSerialize // But provider returns false
            });
            var sut = new ExceptionQueryHandlerDecorator<TestQuery, int>(
                _mockInnerHandler.Object,
                _mockLogger.Object,
                _mockSanitizerPipeline.Object,
                options);

            var query = new TestQuery();
            var innerException = new InvalidOperationException(Any.String());
            _mockInnerHandler.Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
                .ThrowsAsync(innerException);

            try
            {
                await sut.HandleAsync(query);
                Assert.Fail("Expected exception was not thrown");
            }
            catch (QueryHandlerException<TestQuery, int> ex)
            {
                ex.Message.Should().Contain("serialization disabled");
            }

            // Verify sanitizer was NOT called (provider returned false)
            _mockSanitizerPipeline.Verify(p => p.Sanitize(It.IsAny<object>()), Times.Never);
        }
    }

    public class TestQuery : IQuery<int>
    {
        public Guid TraceId { get; set; } = Any.Guid();
    }
}

