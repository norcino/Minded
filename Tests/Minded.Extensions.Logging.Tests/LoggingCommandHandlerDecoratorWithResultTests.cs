using AnonymousData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.DataProtection.Abstractions;
using Minded.Extensions.Logging.Configuration;
using Minded.Extensions.Logging.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Minded.Extensions.Logging.Tests
{
    /// <summary>
    /// Unit tests for LoggingCommandHandlerDecorator (generic version with TResult).
    /// Tests logging behavior, delegation, and configuration handling.
    /// </summary>
    [TestClass]
    public class LoggingCommandHandlerDecoratorWithResultTests
    {
        private Mock<ICommandHandler<TestCommandWithResult, string>> _mockInnerHandler;
        private Mock<ILogger<LoggingCommandHandlerDecorator<TestCommandWithResult, string>>> _mockLogger;
        private Mock<IOptions<LoggingOptions>> _mockOptions;
        private Mock<IDataSanitizer> _mockDataSanitizer;
        private LoggingCommandHandlerDecorator<TestCommandWithResult, string> _sut;

        [TestInitialize]
        public void Setup()
        {
            _mockInnerHandler = new Mock<ICommandHandler<TestCommandWithResult, string>>();
            _mockLogger = new Mock<ILogger<LoggingCommandHandlerDecorator<TestCommandWithResult, string>>>();
            _mockOptions = new Mock<IOptions<LoggingOptions>>();
            _mockOptions.Setup(o => o.Value).Returns(new LoggingOptions { Enabled = true });
            _mockDataSanitizer = new Mock<IDataSanitizer>();
            _sut = new LoggingCommandHandlerDecorator<TestCommandWithResult, string>(
                _mockInnerHandler.Object,
                _mockLogger.Object,
                _mockOptions.Object,
                _mockDataSanitizer.Object);
        }

        /// <summary>
        /// Tests that HandleAsync delegates to inner handler when logging is enabled.
        /// Verifies successful response with result is returned and logged.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenLoggingEnabled_DelegatesToInnerHandlerAndLogs()
        {
            var command = new TestCommandWithResult();
            var expectedResult = Any.String();
            var expectedResponse = new CommandResponse<string>(expectedResult);
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            var result = await _sut.HandleAsync(command);

            result.Should().Be(expectedResponse);
            result.Result.Should().Be(expectedResult);
            _mockInnerHandler.Verify(h => h.HandleAsync(command, It.IsAny<CancellationToken>()), Times.Once);
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
            var command = new TestCommandWithResult();
            var expectedResponse = new CommandResponse<string>(Any.String());
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            var result = await _sut.HandleAsync(command);

            result.Should().Be(expectedResponse);
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
            var command = new TestCommandWithResult();
            var exception = new InvalidOperationException(Any.String());
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            Func<Task> act = async () => await _sut.HandleAsync(command);

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
            var command = new TestCommandWithResult();
            var cancellationToken = new CancellationToken();
            var expectedResponse = new CommandResponse<string>(Any.String());
            _mockInnerHandler.Setup(h => h.HandleAsync(command, cancellationToken))
                .ReturnsAsync(expectedResponse);

            await _sut.HandleAsync(command, cancellationToken);

            _mockInnerHandler.Verify(h => h.HandleAsync(command, cancellationToken), Times.Once);
        }
    }

    public class TestCommandWithResult : ICommand<string>
    {
        public Guid TraceId { get; set; } = Any.Guid();
    }
}

