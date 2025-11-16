using System;
using System.Threading;
using System.Threading.Tasks;
using AnonymousData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minded.Extensions.Retry.Configuration;
using Minded.Extensions.Retry.Decorator;
using Minded.Framework.CQRS.Command;
using Moq;

namespace Minded.Extensions.Retry.Tests
{
    /// <summary>
    /// Unit tests for RetryCommandHandlerDecorator (commands without result).
    /// Verifies retry logic, delay handling, and attribute configuration.
    /// </summary>
    [TestClass]
    public class RetryCommandHandlerDecoratorTests
    {
        [TestMethod]
        public async Task HandleAsync_ExecutesSuccessfully_WhenNoAttributePresent()
        {
            var mockInnerHandler = new Mock<ICommandHandler<TestCommand>>();
            var mockLogger = new Mock<ILogger<RetryCommandHandlerDecorator<TestCommand>>>();
            var options = Options.Create(new RetryOptions());
            var sut = new RetryCommandHandlerDecorator<TestCommand>(mockInnerHandler.Object, mockLogger.Object, options);

            var command = new TestCommand();
            var expectedResponse = new CommandResponse { Successful = true };
            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            var result = await sut.HandleAsync(command, CancellationToken.None);

            result.Should().Be(expectedResponse);
            mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task HandleAsync_ExecutesSuccessfully_WhenAttributePresentAndNoFailure()
        {
            var mockInnerHandler = new Mock<ICommandHandler<TestCommandWithRetry>>();
            var mockLogger = new Mock<ILogger<RetryCommandHandlerDecorator<TestCommandWithRetry>>>();
            var options = Options.Create(new RetryOptions());
            var sut = new RetryCommandHandlerDecorator<TestCommandWithRetry>(mockInnerHandler.Object, mockLogger.Object, options);

            var command = new TestCommandWithRetry();
            var expectedResponse = new CommandResponse { Successful = true };
            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestCommandWithRetry>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            var result = await sut.HandleAsync(command, CancellationToken.None);

            result.Should().Be(expectedResponse);
            mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestCommandWithRetry>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task HandleAsync_RetriesOnException_WhenAttributePresent()
        {
            var mockInnerHandler = new Mock<ICommandHandler<TestCommandWithRetry>>();
            var mockLogger = new Mock<ILogger<RetryCommandHandlerDecorator<TestCommandWithRetry>>>();
            var options = Options.Create(new RetryOptions());
            var sut = new RetryCommandHandlerDecorator<TestCommandWithRetry>(mockInnerHandler.Object, mockLogger.Object, options);

            var command = new TestCommandWithRetry();
            var expectedResponse = new CommandResponse { Successful = true };
            var callCount = 0;

            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestCommandWithRetry>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                        throw new InvalidOperationException("First attempt failed");
                    return Task.FromResult<ICommandResponse>(expectedResponse);
                });

            var result = await sut.HandleAsync(command, CancellationToken.None);

            result.Should().Be(expectedResponse);
            mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestCommandWithRetry>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task HandleAsync_ThrowsException_WhenAllRetriesExhausted()
        {
            var mockInnerHandler = new Mock<ICommandHandler<TestCommandWithRetry>>();
            var mockLogger = new Mock<ILogger<RetryCommandHandlerDecorator<TestCommandWithRetry>>>();
            var options = Options.Create(new RetryOptions());
            var sut = new RetryCommandHandlerDecorator<TestCommandWithRetry>(mockInnerHandler.Object, mockLogger.Object, options);

            var command = new TestCommandWithRetry();
            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestCommandWithRetry>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Persistent failure"));

            var act = async () => await sut.HandleAsync(command, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Persistent failure");
            mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestCommandWithRetry>(), It.IsAny<CancellationToken>()), Times.Exactly(4)); // 1 initial + 3 retries
        }

        [TestMethod]
        public async Task HandleAsync_UsesAttributeRetryCount_WhenSpecified()
        {
            var mockInnerHandler = new Mock<ICommandHandler<TestCommandWithCustomRetryCount>>();
            var mockLogger = new Mock<ILogger<RetryCommandHandlerDecorator<TestCommandWithCustomRetryCount>>>();
            var options = Options.Create(new RetryOptions());
            var sut = new RetryCommandHandlerDecorator<TestCommandWithCustomRetryCount>(mockInnerHandler.Object, mockLogger.Object, options);

            var command = new TestCommandWithCustomRetryCount();
            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestCommandWithCustomRetryCount>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            var act = async () => await sut.HandleAsync(command, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestCommandWithCustomRetryCount>(), It.IsAny<CancellationToken>()), Times.Exactly(6)); // 1 initial + 5 retries
        }

        [TestMethod]
        public async Task HandleAsync_UsesDefaultRetryCount_WhenAttributeDoesNotSpecify()
        {
            var mockInnerHandler = new Mock<ICommandHandler<TestCommandWithRetry>>();
            var mockLogger = new Mock<ILogger<RetryCommandHandlerDecorator<TestCommandWithRetry>>>();
            var options = Options.Create(new RetryOptions { DefaultRetryCount = 2 });
            var sut = new RetryCommandHandlerDecorator<TestCommandWithRetry>(mockInnerHandler.Object, mockLogger.Object, options);

            var command = new TestCommandWithRetry();
            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestCommandWithRetry>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            var act = async () => await sut.HandleAsync(command, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestCommandWithRetry>(), It.IsAny<CancellationToken>()), Times.Exactly(3)); // 1 initial + 2 retries
        }

        [TestMethod]
        public async Task HandleAsync_AppliesDelay_WhenSpecifiedInAttribute()
        {
            var mockInnerHandler = new Mock<ICommandHandler<TestCommandWithDelay>>();
            var mockLogger = new Mock<ILogger<RetryCommandHandlerDecorator<TestCommandWithDelay>>>();
            var options = Options.Create(new RetryOptions());
            var sut = new RetryCommandHandlerDecorator<TestCommandWithDelay>(mockInnerHandler.Object, mockLogger.Object, options);

            var command = new TestCommandWithDelay();
            var callCount = 0;
            var expectedResponse = new CommandResponse { Successful = true };

            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestCommandWithDelay>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                        throw new InvalidOperationException("First attempt failed");
                    return Task.FromResult<ICommandResponse>(expectedResponse);
                });

            var startTime = DateTime.UtcNow;
            var result = await sut.HandleAsync(command, CancellationToken.None);
            var elapsed = DateTime.UtcNow - startTime;

            result.Should().Be(expectedResponse);
            elapsed.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(90); // 100ms delay with some tolerance
        }

    }

    // Test command classes
    public class TestCommand : ICommand
    {
        public Guid TraceId { get; } = Guid.NewGuid();
    }

    [RetryCommand]
    public class TestCommandWithRetry : ICommand
    {
        public Guid TraceId { get; } = Guid.NewGuid();
    }

    [RetryCommand(5)]
    public class TestCommandWithCustomRetryCount : ICommand
    {
        public Guid TraceId { get; } = Guid.NewGuid();
    }

    [RetryCommand(3, 100)]
    public class TestCommandWithDelay : ICommand
    {
        public Guid TraceId { get; } = Guid.NewGuid();
    }
}

