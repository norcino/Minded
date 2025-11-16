using AnonymousData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    /// Unit tests for LoggingCommandHandlerDecorator (non-generic version).
    /// Tests logging behavior, delegation, and configuration handling.
    /// </summary>
    [TestClass]
    public class LoggingCommandHandlerDecoratorTests
    {
        private Mock<ICommandHandler<TestCommand>> _mockInnerHandler;
        private Mock<ILogger<LoggingCommandHandlerDecorator<TestCommand>>> _mockLogger;
        private Mock<IOptions<LoggingOptions>> _mockOptions;
        private LoggingCommandHandlerDecorator<TestCommand> _sut;

        [TestInitialize]
        public void Setup()
        {
            _mockInnerHandler = new Mock<ICommandHandler<TestCommand>>();
            _mockLogger = new Mock<ILogger<LoggingCommandHandlerDecorator<TestCommand>>>();
            _mockOptions = new Mock<IOptions<LoggingOptions>>();
            _mockOptions.Setup(o => o.Value).Returns(new LoggingOptions { Enabled = true });
            _sut = new LoggingCommandHandlerDecorator<TestCommand>(
                _mockInnerHandler.Object,
                _mockLogger.Object,
                _mockOptions.Object);
        }

        /// <summary>
        /// Tests that HandleAsync delegates to inner handler when logging is enabled.
        /// Verifies successful response is returned and logged.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenLoggingEnabled_DelegatesToInnerHandlerAndLogs()
        {
            var command = new TestCommand();
            var expectedResponse = new CommandResponse { Successful = true };
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            var result = await _sut.HandleAsync(command);

            result.Should().Be(expectedResponse);
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
            var command = new TestCommand();
            var expectedResponse = new CommandResponse { Successful = true };
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
            var command = new TestCommand();
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
            var command = new TestCommand();
            var cancellationToken = new CancellationToken();
            var expectedResponse = new CommandResponse { Successful = true };
            _mockInnerHandler.Setup(h => h.HandleAsync(command, cancellationToken))
                .ReturnsAsync(expectedResponse);

            await _sut.HandleAsync(command, cancellationToken);

            _mockInnerHandler.Verify(h => h.HandleAsync(command, cancellationToken), Times.Once);
        }

        /// <summary>
        /// Tests that outcome entries are logged when LogOutcomeEntries is enabled.
        /// Verifies outcome entries are logged with correct severity levels.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenLogOutcomeEntriesEnabled_LogsOutcomeEntries()
        {
            _mockOptions.Setup(o => o.Value).Returns(new LoggingOptions
            {
                Enabled = true,
                LogOutcomeEntries = true,
                MinimumOutcomeSeverityLevel = Severity.Info
            });
            var command = new TestCommand();
            var response = new CommandResponse
            {
                Successful = true,
                OutcomeEntries = new System.Collections.Generic.List<IOutcomeEntry>
                {
                    new OutcomeEntry("Property1", "Error message", null, Severity.Error, "ERR001"),
                    new OutcomeEntry("Property2", "Warning message", null, Severity.Warning, "WARN001"),
                    new OutcomeEntry("Property3", "Info message", null, Severity.Info, "INFO001")
                }
            };
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            await _sut.HandleAsync(command);

            // Verify all three outcome entries are logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error message")),
                    null,
                    It.IsAny<Func<It.IsAnyType, System.Exception, string>>()),
                Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Warning message")),
                    null,
                    It.IsAny<Func<It.IsAnyType, System.Exception, string>>()),
                Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Info message")),
                    null,
                    It.IsAny<Func<It.IsAnyType, System.Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that outcome entries are filtered by minimum severity level.
        /// Verifies only entries with severity <= minimum are logged.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenMinimumSeverityIsWarning_OnlyLogsErrorAndWarning()
        {
            _mockOptions.Setup(o => o.Value).Returns(new LoggingOptions
            {
                Enabled = true,
                LogOutcomeEntries = true,
                MinimumOutcomeSeverityLevel = Severity.Warning
            });
            var command = new TestCommand();
            var response = new CommandResponse
            {
                Successful = true,
                OutcomeEntries = new System.Collections.Generic.List<IOutcomeEntry>
                {
                    new OutcomeEntry("Property1", "Error message", null, Severity.Error),
                    new OutcomeEntry("Property2", "Warning message", null, Severity.Warning),
                    new OutcomeEntry("Property3", "Info message", null, Severity.Info)
                }
            };
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            await _sut.HandleAsync(command);

            // Verify Error and Warning are logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error message")),
                    null,
                    It.IsAny<Func<It.IsAnyType, System.Exception, string>>()),
                Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Warning message")),
                    null,
                    It.IsAny<Func<It.IsAnyType, System.Exception, string>>()),
                Times.Once);
            // Verify Info is NOT logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Info message")),
                    null,
                    It.IsAny<Func<It.IsAnyType, System.Exception, string>>()),
                Times.Never);
        }

        /// <summary>
        /// Tests that outcome entries are not logged when LogOutcomeEntries is disabled.
        /// Verifies outcome logging can be disabled independently.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenLogOutcomeEntriesDisabled_DoesNotLogOutcomeEntries()
        {
            _mockOptions.Setup(o => o.Value).Returns(new LoggingOptions
            {
                Enabled = true,
                LogOutcomeEntries = false
            });
            var command = new TestCommand();
            var response = new CommandResponse
            {
                Successful = true,
                OutcomeEntries = new System.Collections.Generic.List<IOutcomeEntry>
                {
                    new OutcomeEntry("Property1", "Error message", null, Severity.Error)
                }
            };
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            await _sut.HandleAsync(command);

            // Verify outcome entry is NOT logged (only Started and Completed are logged)
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error message")),
                    null,
                    It.IsAny<Func<It.IsAnyType, System.Exception, string>>()),
                Times.Never);
        }

        /// <summary>
        /// Tests that dynamic severity level provider is used when set.
        /// Verifies provider function is called and its value is used for filtering.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenSeverityLevelProviderSet_UsesDynamicValue()
        {
            var dynamicSeverity = Severity.Info;
            _mockOptions.Setup(o => o.Value).Returns(new LoggingOptions
            {
                Enabled = true,
                LogOutcomeEntries = true,
                MinimumOutcomeSeverityLevel = Severity.Error, // Static value
                MinimumOutcomeSeverityLevelProvider = () => dynamicSeverity // Dynamic value takes precedence
            });
            var command = new TestCommand();
            var response = new CommandResponse
            {
                Successful = true,
                OutcomeEntries = new System.Collections.Generic.List<IOutcomeEntry>
                {
                    new OutcomeEntry("Property1", "Info message", null, Severity.Info)
                }
            };
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            await _sut.HandleAsync(command);

            // Verify Info is logged (because provider returns Info, not Error)
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Info message")),
                    null,
                    It.IsAny<Func<It.IsAnyType, System.Exception, string>>()),
                Times.Once);
        }
    }

    public class TestCommand : ICommand
    {
        public Guid TraceId { get; set; } = Any.Guid();
    }
}

