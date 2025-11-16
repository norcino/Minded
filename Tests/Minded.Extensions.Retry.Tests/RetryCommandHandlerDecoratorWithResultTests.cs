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
    /// Unit tests for RetryCommandHandlerDecorator (commands with result).
    /// Verifies retry logic, delay handling, and attribute configuration for commands returning results.
    /// </summary>
    [TestClass]
    public class RetryCommandHandlerDecoratorWithResultTests
    {
        [TestMethod]
        public async Task HandleAsync_ExecutesSuccessfully_WhenNoAttributePresent()
        {
            var mockInnerHandler = new Mock<ICommandHandler<TestCommandWithResult, int>>();
            var mockLogger = new Mock<ILogger<RetryCommandHandlerDecorator<TestCommandWithResult, int>>>();
            var options = Options.Create(new RetryOptions());
            var sut = new RetryCommandHandlerDecorator<TestCommandWithResult, int>(mockInnerHandler.Object, mockLogger.Object, options);

            var command = new TestCommandWithResult();
            var expectedResult = Any.Int();
            var expectedResponse = new CommandResponse<int>(expectedResult) { Successful = true };
            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestCommandWithResult>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            var result = await sut.HandleAsync(command, CancellationToken.None);

            result.Should().Be(expectedResponse);
            result.Result.Should().Be(expectedResult);
            mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestCommandWithResult>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task HandleAsync_ExecutesSuccessfully_WhenAttributePresentAndNoFailure()
        {
            var mockInnerHandler = new Mock<ICommandHandler<TestCommandWithResultAndRetry, int>>();
            var mockLogger = new Mock<ILogger<RetryCommandHandlerDecorator<TestCommandWithResultAndRetry, int>>>();
            var options = Options.Create(new RetryOptions());
            var sut = new RetryCommandHandlerDecorator<TestCommandWithResultAndRetry, int>(mockInnerHandler.Object, mockLogger.Object, options);

            var command = new TestCommandWithResultAndRetry();
            var expectedResult = Any.Int();
            var expectedResponse = new CommandResponse<int>(expectedResult) { Successful = true };
            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestCommandWithResultAndRetry>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            var result = await sut.HandleAsync(command, CancellationToken.None);

            result.Should().Be(expectedResponse);
            result.Result.Should().Be(expectedResult);
            mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestCommandWithResultAndRetry>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task HandleAsync_RetriesOnException_WhenAttributePresent()
        {
            var mockInnerHandler = new Mock<ICommandHandler<TestCommandWithResultAndRetry, int>>();
            var mockLogger = new Mock<ILogger<RetryCommandHandlerDecorator<TestCommandWithResultAndRetry, int>>>();
            var options = Options.Create(new RetryOptions());
            var sut = new RetryCommandHandlerDecorator<TestCommandWithResultAndRetry, int>(mockInnerHandler.Object, mockLogger.Object, options);

            var command = new TestCommandWithResultAndRetry();
            var expectedResult = Any.Int();
            var expectedResponse = new CommandResponse<int>(expectedResult) { Successful = true };
            var callCount = 0;

            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestCommandWithResultAndRetry>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                        throw new InvalidOperationException("First attempt failed");
                    return Task.FromResult<ICommandResponse<int>>(expectedResponse);
                });

            var result = await sut.HandleAsync(command, CancellationToken.None);

            result.Should().Be(expectedResponse);
            result.Result.Should().Be(expectedResult);
            mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestCommandWithResultAndRetry>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task HandleAsync_ThrowsException_WhenAllRetriesExhausted()
        {
            var mockInnerHandler = new Mock<ICommandHandler<TestCommandWithResultAndRetry, int>>();
            var mockLogger = new Mock<ILogger<RetryCommandHandlerDecorator<TestCommandWithResultAndRetry, int>>>();
            var options = Options.Create(new RetryOptions());
            var sut = new RetryCommandHandlerDecorator<TestCommandWithResultAndRetry, int>(mockInnerHandler.Object, mockLogger.Object, options);

            var command = new TestCommandWithResultAndRetry();
            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestCommandWithResultAndRetry>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Persistent failure"));

            var act = async () => await sut.HandleAsync(command, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Persistent failure");
            mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestCommandWithResultAndRetry>(), It.IsAny<CancellationToken>()), Times.Exactly(4)); // 1 initial + 3 retries
        }

        [TestMethod]
        public async Task HandleAsync_UsesMultipleDelays_WhenSpecifiedInAttribute()
        {
            var mockInnerHandler = new Mock<ICommandHandler<TestCommandWithResultAndMultipleDelays, int>>();
            var mockLogger = new Mock<ILogger<RetryCommandHandlerDecorator<TestCommandWithResultAndMultipleDelays, int>>>();
            var options = Options.Create(new RetryOptions());
            var sut = new RetryCommandHandlerDecorator<TestCommandWithResultAndMultipleDelays, int>(mockInnerHandler.Object, mockLogger.Object, options);

            var command = new TestCommandWithResultAndMultipleDelays();
            var callCount = 0;
            var expectedResult = Any.Int();
            var expectedResponse = new CommandResponse<int>(expectedResult) { Successful = true };

            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestCommandWithResultAndMultipleDelays>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount <= 2)
                        throw new InvalidOperationException($"Attempt {callCount} failed");
                    return Task.FromResult<ICommandResponse<int>>(expectedResponse);
                });

            var startTime = DateTime.UtcNow;
            var result = await sut.HandleAsync(command, CancellationToken.None);
            var elapsed = DateTime.UtcNow - startTime;

            result.Should().Be(expectedResponse);
            elapsed.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(140); // 50ms + 100ms with some tolerance
        }

        [TestMethod]
        public async Task HandleAsync_UsesDefaultDelays_WhenAttributeHasNoDelays()
        {
            var mockInnerHandler = new Mock<ICommandHandler<TestCommandWithResultAndRetry, int>>();
            var mockLogger = new Mock<ILogger<RetryCommandHandlerDecorator<TestCommandWithResultAndRetry, int>>>();
            var options = Options.Create(new RetryOptions { DefaultDelay1 = 50 });
            var sut = new RetryCommandHandlerDecorator<TestCommandWithResultAndRetry, int>(mockInnerHandler.Object, mockLogger.Object, options);

            var command = new TestCommandWithResultAndRetry();
            var callCount = 0;
            var expectedResult = Any.Int();
            var expectedResponse = new CommandResponse<int>(expectedResult) { Successful = true };

            mockInnerHandler.Setup(h => h.HandleAsync(It.IsAny<TestCommandWithResultAndRetry>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                        throw new InvalidOperationException("First attempt failed");
                    return Task.FromResult<ICommandResponse<int>>(expectedResponse);
                });

            var startTime = DateTime.UtcNow;
            var result = await sut.HandleAsync(command, CancellationToken.None);
            var elapsed = DateTime.UtcNow - startTime;

            result.Should().Be(expectedResponse);
            elapsed.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(40); // 50ms with some tolerance
        }

    }

    // Test command classes
    public class TestCommandWithResult : ICommand<int>
    {
        public Guid TraceId { get; } = Guid.NewGuid();
    }

    [RetryCommand]
    public class TestCommandWithResultAndRetry : ICommand<int>
    {
        public Guid TraceId { get; } = Guid.NewGuid();
    }

    [RetryCommand(3, 50, 100)]
    public class TestCommandWithResultAndMultipleDelays : ICommand<int>
    {
        public Guid TraceId { get; } = Guid.NewGuid();
    }
}

