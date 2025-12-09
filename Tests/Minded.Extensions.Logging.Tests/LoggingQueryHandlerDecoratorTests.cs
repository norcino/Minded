using AnonymousData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.DataProtection.Abstractions;
using Minded.Extensions.Logging.Configuration;
using Minded.Extensions.Logging.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Query;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Minded.Extensions.Logging.Tests
{
    /// <summary>
    /// Unit tests for LoggingQueryHandlerDecorator.
    /// Tests logging behavior, delegation, and configuration handling.
    /// </summary>
    [TestClass]
    public class LoggingQueryHandlerDecoratorTests
    {
        private Mock<IQueryHandler<TestQuery, int>> _mockInnerHandler;
        private Mock<ILogger<LoggingQueryHandlerDecorator<TestQuery, int>>> _mockLogger;
        private Mock<IOptions<LoggingOptions>> _mockOptions;
        private Mock<IDataSanitizer> _mockDataSanitizer;
        private LoggingQueryHandlerDecorator<TestQuery, int> _sut;

        [TestInitialize]
        public void Setup()
        {
            _mockInnerHandler = new Mock<IQueryHandler<TestQuery, int>>();
            _mockLogger = new Mock<ILogger<LoggingQueryHandlerDecorator<TestQuery, int>>>();
            _mockOptions = new Mock<IOptions<LoggingOptions>>();
            _mockOptions.Setup(o => o.Value).Returns(new LoggingOptions { Enabled = true });
            _mockDataSanitizer = new Mock<IDataSanitizer>();
            _sut = new LoggingQueryHandlerDecorator<TestQuery, int>(
                _mockInnerHandler.Object,
                _mockLogger.Object,
                _mockOptions.Object,
                _mockDataSanitizer.Object);
        }

        /// <summary>
        /// Tests that HandleAsync delegates to inner handler when logging is enabled.
        /// Verifies successful result is returned and logged.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenLoggingEnabled_DelegatesToInnerHandlerAndLogs()
        {
            var query = new TestQuery();
            var expectedResult = Any.Int();
            _mockInnerHandler.Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var result = await _sut.HandleAsync(query);

            result.Should().Be(expectedResult);
            _mockInnerHandler.Verify(h => h.HandleAsync(query, It.IsAny<CancellationToken>()), Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Started")),
                    null,
                    It.IsAny<Func<It.IsAnyType, System.Exception, string>>()),
                Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Completed")),
                    null,
                    It.IsAny<Func<It.IsAnyType, System.Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that HandleAsync bypasses logging when disabled.
        /// Verifies no logging occurs when Enabled is false.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenLoggingDisabled_BypassesLogging()
        {
            _mockOptions.Setup(o => o.Value).Returns(new LoggingOptions { Enabled = false });
            var query = new TestQuery();
            var expectedResult = Any.Int();
            _mockInnerHandler.Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var result = await _sut.HandleAsync(query);

            result.Should().Be(expectedResult);
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<System.Exception>(),
                    It.IsAny<Func<It.IsAnyType, System.Exception, string>>()),
                Times.Never);
        }

        /// <summary>
        /// Tests that HandleAsync logs exception when inner handler throws.
        /// Verifies exception is logged and re-thrown.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenExceptionThrown_LogsAndRethrows()
        {
            var query = new TestQuery();
            var exception = new InvalidOperationException(Any.String());
            _mockInnerHandler.Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            Func<Task> act = async () => await _sut.HandleAsync(query);

            await act.Should().ThrowAsync<InvalidOperationException>();
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed")),
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
    }

    public class TestQuery : IQuery<int>
    {
        public Guid TraceId { get; set; } = Any.Guid();
    }
}

