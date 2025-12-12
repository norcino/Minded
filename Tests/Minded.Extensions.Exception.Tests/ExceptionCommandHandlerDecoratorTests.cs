using AnonymousData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Exception.Configuration;
using Minded.Extensions.Exception.Decorator;
using Minded.Framework.CQRS;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Abstractions.Sanitization;
using Minded.Framework.CQRS.Command;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Minded.Extensions.Exception.Tests
{
    /// <summary>
    /// Unit tests for ExceptionCommandHandlerDecorator (non-generic version).
    /// Tests exception handling, logging, and delegation to inner handler.
    /// </summary>
    [TestClass]
    public class ExceptionCommandHandlerDecoratorTests
    {
        private Mock<ICommandHandler<TestCommand>> _mockInnerHandler;
        private Mock<ILogger<ExceptionCommandHandlerDecorator<TestCommand>>> _mockLogger;
        private Mock<ILoggingSanitizerPipeline> _mockSanitizerPipeline;
        private IOptions<ExceptionOptions> _options;
        private ExceptionCommandHandlerDecorator<TestCommand> _sut;

        [TestInitialize]
        public void Setup()
        {
            _mockInnerHandler = new Mock<ICommandHandler<TestCommand>>();
            _mockLogger = new Mock<ILogger<ExceptionCommandHandlerDecorator<TestCommand>>>();
            _mockSanitizerPipeline = new Mock<ILoggingSanitizerPipeline>();
            _options = Options.Create(new ExceptionOptions());

            // Setup the pipeline to return a simple dictionary by default
            _mockSanitizerPipeline
                .Setup(p => p.Sanitize(It.IsAny<object>()))
                .Returns(new System.Collections.Generic.Dictionary<string, object>());

            _sut = new ExceptionCommandHandlerDecorator<TestCommand>(
                _mockInnerHandler.Object,
                _mockLogger.Object,
                _mockSanitizerPipeline.Object,
                _options);
        }

        /// <summary>
        /// Tests that HandleAsync delegates to inner handler when no exception occurs.
        /// Verifies successful response is returned.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenNoException_DelegatesToInnerHandler()
        {
            var command = new TestCommand();
            var expectedResponse = new CommandResponse { Successful = true };
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            ICommandResponse result = await _sut.HandleAsync(command);

            result.Should().Be(expectedResponse);
            _mockInnerHandler.Verify(h => h.HandleAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests that HandleAsync wraps exceptions in CommandHandlerException.
        /// Verifies exception is logged and wrapped correctly.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenExceptionThrown_WrapsInCommandHandlerException()
        {
            var command = new TestCommand();
            var innerException = new InvalidOperationException(Any.String());
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ThrowsAsync(innerException);

            Func<Task> act = async () => await _sut.HandleAsync(command);

            await act.Should().ThrowAsync<CommandHandlerException<TestCommand>>()
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
            var command = new TestCommand();
            var cancelledException = new OperationCanceledException();
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ThrowsAsync(cancelledException);

            Func<Task> act = async () => await _sut.HandleAsync(command);

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
            var command = new TestCommand();
            var cancellationToken = new CancellationToken();
            var expectedResponse = new CommandResponse { Successful = true };
            _mockInnerHandler.Setup(h => h.HandleAsync(command, cancellationToken))
                .ReturnsAsync(expectedResponse);

            await _sut.HandleAsync(command, cancellationToken);

            _mockInnerHandler.Verify(h => h.HandleAsync(command, cancellationToken), Times.Once);
        }

        /// <summary>
        /// Tests that CommandHandlerException contains the command instance.
        /// Verifies exception includes command for debugging.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenExceptionThrown_ExceptionContainsCommand()
        {
            var command = new TestCommand();
            var innerException = new InvalidOperationException(Any.String());
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ThrowsAsync(innerException);

            try
            {
                await _sut.HandleAsync(command);
                Assert.Fail("Expected exception was not thrown");
            }
            catch (CommandHandlerException<TestCommand> ex)
            {
                ex.Command.Should().Be(command);
                ex.InnerException.Should().Be(innerException);
            }
        }

        /// <summary>
        /// Tests that command is serialized when Serialize option is true (default).
        /// Verifies serialized JSON is included in exception message.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenSerializeIsTrue_IncludesSerializedCommandInException()
        {
            var command = new TestCommand();
            var innerException = new InvalidOperationException(Any.String());
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ThrowsAsync(innerException);

            var sanitizedData = new System.Collections.Generic.Dictionary<string, object>
            {
                { "TraceId", command.TraceId.ToString() }
            };
            _mockSanitizerPipeline.Setup(p => p.Sanitize(command)).Returns(sanitizedData);

            try
            {
                await _sut.HandleAsync(command);
                Assert.Fail("Expected exception was not thrown");
            }
            catch (CommandHandlerException<TestCommand> ex)
            {
                ex.Message.Should().Contain("CommandHandlerException:");
                ex.Message.Should().Contain(command.TraceId.ToString());
                ex.Message.Should().NotContain("serialization disabled");
            }

            _mockSanitizerPipeline.Verify(p => p.Sanitize(command), Times.Once);
        }

        /// <summary>
        /// Tests that command is NOT serialized when Serialize option is false.
        /// Verifies only type name is included in exception message.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenSerializeIsFalse_IncludesOnlyTypeNameInException()
        {
            // Create decorator with serialization disabled
            var options = Options.Create(new ExceptionOptions { Serialize = false });
            var sut = new ExceptionCommandHandlerDecorator<TestCommand>(
                _mockInnerHandler.Object,
                _mockLogger.Object,
                _mockSanitizerPipeline.Object,
                options);

            var command = new TestCommand();
            var innerException = new InvalidOperationException(Any.String());
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ThrowsAsync(innerException);

            try
            {
                await sut.HandleAsync(command);
                Assert.Fail("Expected exception was not thrown");
            }
            catch (CommandHandlerException<TestCommand> ex)
            {
                ex.Message.Should().Contain("CommandHandlerException:");
                ex.Message.Should().Contain("Type: TestCommand");
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
            var sut = new ExceptionCommandHandlerDecorator<TestCommand>(
                _mockInnerHandler.Object,
                _mockLogger.Object,
                _mockSanitizerPipeline.Object,
                options);

            var command = new TestCommand();
            var innerException = new InvalidOperationException(Any.String());
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ThrowsAsync(innerException);

            try
            {
                await sut.HandleAsync(command);
                Assert.Fail("Expected exception was not thrown");
            }
            catch (CommandHandlerException<TestCommand> ex)
            {
                ex.Message.Should().Contain("serialization disabled");
            }

            // Verify sanitizer was NOT called (provider returned false)
            _mockSanitizerPipeline.Verify(p => p.Sanitize(It.IsAny<object>()), Times.Never);
        }
    }

    public class TestCommand : ICommand
    {
        public Guid TraceId { get; set; } = Any.Guid();
    }
}

