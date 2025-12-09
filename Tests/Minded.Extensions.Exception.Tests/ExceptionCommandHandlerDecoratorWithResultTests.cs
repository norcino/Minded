using AnonymousData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.DataProtection.Abstractions;
using Minded.Extensions.Exception.Decorator;
using Minded.Framework.CQRS;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Minded.Extensions.Exception.Tests
{
    /// <summary>
    /// Unit tests for ExceptionCommandHandlerDecorator (generic version with TResult).
    /// Tests exception handling, logging, and delegation to inner handler.
    /// </summary>
    [TestClass]
    public class ExceptionCommandHandlerDecoratorWithResultTests
    {
        private Mock<ICommandHandler<TestCommandWithResult, string>> _mockInnerHandler;
        private Mock<ILogger<ExceptionCommandHandlerDecorator<TestCommandWithResult, string>>> _mockLogger;
        private Mock<IDataSanitizer> _mockDataSanitizer;
        private Mock<IOptions<DataProtectionOptions>> _mockOptions;
        private ExceptionCommandHandlerDecorator<TestCommandWithResult, string> _sut;

        [TestInitialize]
        public void Setup()
        {
            _mockInnerHandler = new Mock<ICommandHandler<TestCommandWithResult, string>>();
            _mockLogger = new Mock<ILogger<ExceptionCommandHandlerDecorator<TestCommandWithResult, string>>>();
            _mockDataSanitizer = new Mock<IDataSanitizer>();
            _mockOptions = new Mock<IOptions<DataProtectionOptions>>();
            _mockOptions.Setup(o => o.Value).Returns(new DataProtectionOptions());
            _sut = new ExceptionCommandHandlerDecorator<TestCommandWithResult, string>(
                _mockInnerHandler.Object,
                _mockLogger.Object,
                _mockDataSanitizer.Object,
                _mockOptions.Object);
        }

        /// <summary>
        /// Tests that HandleAsync delegates to inner handler when no exception occurs.
        /// Verifies successful response with result is returned.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenNoException_DelegatesToInnerHandler()
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
        }

        /// <summary>
        /// Tests that HandleAsync wraps exceptions in CommandHandlerException.
        /// Verifies exception is logged and wrapped correctly.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenExceptionThrown_WrapsInCommandHandlerException()
        {
            var command = new TestCommandWithResult();
            var innerException = new InvalidOperationException(Any.String());
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ThrowsAsync(innerException);

            Func<Task> act = async () => await _sut.HandleAsync(command);

            await act.Should().ThrowAsync<CommandHandlerException<TestCommandWithResult>>()
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
            var command = new TestCommandWithResult();
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
            var command = new TestCommandWithResult();
            var cancellationToken = new CancellationToken();
            var expectedResponse = new CommandResponse<string>(Any.String());
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
            var command = new TestCommandWithResult();
            var innerException = new InvalidOperationException(Any.String());
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ThrowsAsync(innerException);

            try
            {
                await _sut.HandleAsync(command);
                Assert.Fail("Expected exception was not thrown");
            }
            catch (CommandHandlerException<TestCommandWithResult> ex)
            {
                ex.Command.Should().Be(command);
                ex.InnerException.Should().Be(innerException);
            }
        }
    }

    public class TestCommandWithResult : ICommand<string>
    {
        public Guid TraceId { get; set; } = Any.Guid();
    }
}

