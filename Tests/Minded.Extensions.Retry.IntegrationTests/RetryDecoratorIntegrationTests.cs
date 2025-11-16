using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Retry.Configuration;
using Minded.Extensions.Retry.Decorator;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;
using Moq;

namespace Minded.Extensions.Retry.IntegrationTests
{
    /// <summary>
    /// Integration tests for the Retry decorator.
    /// Tests the retry decorator with real handlers to verify end-to-end functionality.
    /// </summary>
    [TestClass]
    public class RetryDecoratorIntegrationTests
    {
        [TestInitialize]
        public void Setup()
        {
            TestCommandWithRetryHandler.AttemptCount = 0;
            TestCommandWithoutRetryHandler.AttemptCount = 0;
            TestQueryWithRetryHandler.AttemptCount = 0;
            TestQueryWithoutRetryHandler.AttemptCount = 0;
        }

        /// <summary>
        /// Tests that a command with retry attribute succeeds after transient failures.
        /// </summary>
        [TestMethod]
        public async Task CommandDecorator_WithRetryAttribute_SucceedsAfterRetries()
        {
            var command = new TestCommandWithRetry();
            var handler = new TestCommandWithRetryHandler();
            var options = Options.Create(new RetryOptions { DefaultRetryCount = 3, DefaultDelay1 = 10 });
            var logger = new Mock<ILogger<RetryCommandHandlerDecorator<TestCommandWithRetry>>>().Object;
            var decorator = new RetryCommandHandlerDecorator<TestCommandWithRetry>(handler, logger, options);

            var response = await decorator.HandleAsync(command);

            response.Should().NotBeNull();
            response.Successful.Should().BeTrue();
            TestCommandWithRetryHandler.AttemptCount.Should().Be(4);
        }

        /// <summary>
        /// Tests that a command without retry attribute executes without retry logic.
        /// </summary>
        [TestMethod]
        public async Task CommandDecorator_WithoutRetryAttribute_ExecutesOnce()
        {
            var command = new TestCommandWithoutRetry();
            var handler = new TestCommandWithoutRetryHandler();
            var options = Options.Create(new RetryOptions { DefaultRetryCount = 3 });
            var logger = new Mock<ILogger<RetryCommandHandlerDecorator<TestCommandWithoutRetry>>>().Object;
            var decorator = new RetryCommandHandlerDecorator<TestCommandWithoutRetry>(handler, logger, options);

            Func<Task> act = async () => await decorator.HandleAsync(command);

            await act.Should().ThrowAsync<InvalidOperationException>();
            TestCommandWithoutRetryHandler.AttemptCount.Should().Be(1);
        }

        /// <summary>
        /// Tests that a query with retry attribute succeeds after transient failures.
        /// </summary>
        [TestMethod]
        public async Task QueryDecorator_WithRetryAttribute_SucceedsAfterRetries()
        {
            var query = new TestQueryWithRetry();
            var handler = new TestQueryWithRetryHandler();
            var options = Options.Create(new RetryOptions { DefaultRetryCount = 2, DefaultDelay1 = 5, ApplyToAllQueries = false });
            var logger = new Mock<ILogger<RetryQueryHandlerDecorator<TestQueryWithRetry, int>>>().Object;
            var decorator = new RetryQueryHandlerDecorator<TestQueryWithRetry, int>(handler, logger, options);

            var result = await decorator.HandleAsync(query);

            result.Should().Be(42);
            TestQueryWithRetryHandler.AttemptCount.Should().Be(3);
        }

        /// <summary>
        /// Tests that a query without retry attribute fails immediately when ApplyToAllQueries is false.
        /// </summary>
        [TestMethod]
        public async Task QueryDecorator_WithoutRetryAttribute_FailsImmediately()
        {
            var query = new TestQueryWithoutRetry();
            var handler = new TestQueryWithoutRetryHandler();
            var options = Options.Create(new RetryOptions { DefaultRetryCount = 2, ApplyToAllQueries = false });
            var logger = new Mock<ILogger<RetryQueryHandlerDecorator<TestQueryWithoutRetry, int>>>().Object;
            var decorator = new RetryQueryHandlerDecorator<TestQueryWithoutRetry, int>(handler, logger, options);

            Func<Task> act = async () => await decorator.HandleAsync(query);

            await act.Should().ThrowAsync<InvalidOperationException>();
            TestQueryWithoutRetryHandler.AttemptCount.Should().Be(1);
        }

        /// <summary>
        /// Tests that retry decorator respects custom delay intervals.
        /// </summary>
        [TestMethod]
        public async Task CommandDecorator_WithCustomDelays_RespectsDelayIntervals()
        {
            var command = new TestCommandWithCustomDelays();
            var handler = new TestCommandWithCustomDelaysHandler();
            var options = Options.Create(new RetryOptions { DefaultRetryCount = 2 });
            var logger = new Mock<ILogger<RetryCommandHandlerDecorator<TestCommandWithCustomDelays>>>().Object;
            var decorator = new RetryCommandHandlerDecorator<TestCommandWithCustomDelays>(handler, logger, options);
            var startTime = DateTime.UtcNow;

            var response = await decorator.HandleAsync(command);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            response.Successful.Should().BeTrue();
            elapsed.Should().BeGreaterThanOrEqualTo(100 + 200);
        }
    }

    #region Test Commands and Handlers

    [RetryCommand(3, 10, 20, 30)]
    public class TestCommandWithRetry : ICommand
    {
        public Guid TraceId { get; } = Guid.NewGuid();
    }

    public class TestCommandWithRetryHandler : ICommandHandler<TestCommandWithRetry>
    {
        public static int AttemptCount = 0;

        public async Task<ICommandResponse> HandleAsync(TestCommandWithRetry command, CancellationToken cancellationToken = default)
        {
            AttemptCount++;
            if (AttemptCount < 4)
            {
                throw new InvalidOperationException($"Simulated failure (Attempt {AttemptCount})");
            }

            return await Task.FromResult(new CommandResponse { Successful = true });
        }
    }

    public class TestCommandWithoutRetry : ICommand
    {
        public Guid TraceId { get; } = Guid.NewGuid();
    }

    public class TestCommandWithoutRetryHandler : ICommandHandler<TestCommandWithoutRetry>
    {
        public static int AttemptCount = 0;

        public Task<ICommandResponse> HandleAsync(TestCommandWithoutRetry command, CancellationToken cancellationToken = default)
        {
            AttemptCount++;
            throw new InvalidOperationException("This command always fails");
        }
    }

    [RetryCommand(2, 100, 200)]
    public class TestCommandWithCustomDelays : ICommand
    {
        public Guid TraceId { get; } = Guid.NewGuid();
    }

    public class TestCommandWithCustomDelaysHandler : ICommandHandler<TestCommandWithCustomDelays>
    {
        private static int _attemptCount = 0;

        public async Task<ICommandResponse> HandleAsync(TestCommandWithCustomDelays command, CancellationToken cancellationToken = default)
        {
            _attemptCount++;
            if (_attemptCount < 3)
            {
                throw new InvalidOperationException($"Simulated failure (Attempt {_attemptCount})");
            }

            return await Task.FromResult(new CommandResponse { Successful = true });
        }
    }

    [RetryQuery(2, 5)]
    public class TestQueryWithRetry : IQuery<int>
    {
        public Guid TraceId { get; } = Guid.NewGuid();
    }

    public class TestQueryWithRetryHandler : IQueryHandler<TestQueryWithRetry, int>
    {
        public static int AttemptCount = 0;

        public async Task<int> HandleAsync(TestQueryWithRetry query, CancellationToken cancellationToken = default)
        {
            AttemptCount++;
            if (AttemptCount < 3)
            {
                throw new InvalidOperationException($"Simulated failure (Attempt {AttemptCount})");
            }

            return await Task.FromResult(42);
        }
    }

    public class TestQueryWithoutRetry : IQuery<int>
    {
        public Guid TraceId { get; } = Guid.NewGuid();
    }

    public class TestQueryWithoutRetryHandler : IQueryHandler<TestQueryWithoutRetry, int>
    {
        public static int AttemptCount = 0;

        public Task<int> HandleAsync(TestQueryWithoutRetry query, CancellationToken cancellationToken = default)
        {
            AttemptCount++;
            throw new InvalidOperationException("This query always fails");
        }
    }

    #endregion
}

