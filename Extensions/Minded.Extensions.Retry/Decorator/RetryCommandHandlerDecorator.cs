using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minded.Extensions.Retry.Configuration;
using Minded.Framework.CQRS.Command;
using Minded.Framework.Decorator;

namespace Minded.Extensions.Retry.Decorator
{
    /// <summary>
    /// Decorator which implements retry logic for commands without result.
    /// Retries command execution based on RetryCommandAttribute configuration or default options.
    /// </summary>
    /// <typeparam name="TCommand">Command type being handled</typeparam>
    public class RetryCommandHandlerDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand> 
        where TCommand : ICommand
    {
        private readonly ILogger<RetryCommandHandlerDecorator<TCommand>> _logger;
        private readonly IOptions<RetryOptions> _options;

        /// <summary>
        /// Initializes a new instance of the RetryCommandHandlerDecorator class.
        /// </summary>
        /// <param name="decoratedCommandHandler">The decorated command handler</param>
        /// <param name="logger">Logger instance for logging retry attempts</param>
        /// <param name="options">Retry configuration options</param>
        public RetryCommandHandlerDecorator(
            ICommandHandler<TCommand> decoratedCommandHandler,
            ILogger<RetryCommandHandlerDecorator<TCommand>> logger,
            IOptions<RetryOptions> options) : base(decoratedCommandHandler)
        {
            _logger = logger;
            _options = options;
        }

        /// <summary>
        /// Handles the command with retry logic if the command has the RetryCommandAttribute.
        /// </summary>
        /// <param name="command">The command to handle</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Command response</returns>
        public async Task<ICommandResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            var attribute = (RetryCommandAttribute)TypeDescriptor.GetAttributes(command)[typeof(RetryCommandAttribute)];

            // If the command doesn't have the RetryCommandAttribute, just execute it normally
            if (attribute == null)
            {
                return await InnerCommandHandler.HandleAsync(command, cancellationToken);
            }

            var retryCount = attribute.RetryCount ?? _options.Value.DefaultRetryCount;
            var attempt = 0;
            Exception lastException = null;

            while (attempt <= retryCount)
            {
                try
                {
                    if (attempt > 0)
                    {
                        _logger.LogInformation(
                            "[Tracking:{TraceId}] {CommandName:l} - Retry attempt {Attempt} of {MaxRetries}",
                            command.TraceId,
                            typeof(TCommand).Name,
                            attempt,
                            retryCount);
                    }

                    ICommandResponse response = await InnerCommandHandler.HandleAsync(command, cancellationToken);

                    if (attempt > 0)
                    {
                        _logger.LogInformation(
                            "[Tracking:{TraceId}] {CommandName:l} - Retry attempt {Attempt} succeeded",
                            command.TraceId,
                            typeof(TCommand).Name,
                            attempt);
                    }

                    return response;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    attempt++;

                    if (attempt > retryCount)
                    {
                        _logger.LogError(
                            ex,
                            "[Tracking:{TraceId}] {CommandName:l} - All retry attempts exhausted ({MaxRetries}). Throwing exception.",
                            command.TraceId,
                            typeof(TCommand).Name,
                            retryCount);
                        throw;
                    }

                    var delay = attribute.GetDelayForIteration(attempt);
                    if (delay == 0)
                    {
                        delay = _options.Value.GetDefaultDelayForIteration(attempt);
                    }

                    _logger.LogWarning(
                        ex,
                        "[Tracking:{TraceId}] {CommandName:l} - Attempt {Attempt} failed. Retrying in {Delay}ms...",
                        command.TraceId,
                        typeof(TCommand).Name,
                        attempt,
                        delay);

                    if (delay > 0)
                    {
                        await Task.Delay(delay, cancellationToken);
                    }
                }
            }

            // This should never be reached, but just in case
            throw lastException ?? new InvalidOperationException("Retry logic failed unexpectedly");
        }
    }

    /// <summary>
    /// Decorator which implements retry logic for commands with result.
    /// Retries command execution based on RetryCommandAttribute configuration or default options.
    /// </summary>
    /// <typeparam name="TCommand">Command type being handled</typeparam>
    /// <typeparam name="TResult">Result type returned by the command</typeparam>
    public class RetryCommandHandlerDecorator<TCommand, TResult> : CommandHandlerDecoratorBase<TCommand, TResult>, ICommandHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        private readonly ILogger<RetryCommandHandlerDecorator<TCommand, TResult>> _logger;
        private readonly IOptions<RetryOptions> _options;

        /// <summary>
        /// Initializes a new instance of the RetryCommandHandlerDecorator class.
        /// </summary>
        /// <param name="decoratedCommandHandler">The decorated command handler</param>
        /// <param name="logger">Logger instance for logging retry attempts</param>
        /// <param name="options">Retry configuration options</param>
        public RetryCommandHandlerDecorator(
            ICommandHandler<TCommand, TResult> decoratedCommandHandler,
            ILogger<RetryCommandHandlerDecorator<TCommand, TResult>> logger,
            IOptions<RetryOptions> options) : base(decoratedCommandHandler)
        {
            _logger = logger;
            _options = options;
        }

        /// <summary>
        /// Handles the command with retry logic if the command has the RetryCommandAttribute.
        /// </summary>
        /// <param name="command">The command to handle</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Command response with result</returns>
        public async Task<ICommandResponse<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            var attribute = (RetryCommandAttribute)TypeDescriptor.GetAttributes(command)[typeof(RetryCommandAttribute)];

            // If the command doesn't have the RetryCommandAttribute, just execute it normally
            if (attribute == null)
            {
                return await InnerCommandHandler.HandleAsync(command, cancellationToken);
            }

            var retryCount = attribute.RetryCount ?? _options.Value.DefaultRetryCount;
            var attempt = 0;
            Exception lastException = null;

            while (attempt <= retryCount)
            {
                try
                {
                    if (attempt > 0)
                    {
                        _logger.LogInformation(
                            "[Tracking:{TraceId}] {CommandName:l} - Retry attempt {Attempt} of {MaxRetries}",
                            command.TraceId,
                            typeof(TCommand).Name,
                            attempt,
                            retryCount);
                    }

                    ICommandResponse<TResult> response = await InnerCommandHandler.HandleAsync(command, cancellationToken);

                    if (attempt > 0)
                    {
                        _logger.LogInformation(
                            "[Tracking:{TraceId}] {CommandName:l} - Retry attempt {Attempt} succeeded",
                            command.TraceId,
                            typeof(TCommand).Name,
                            attempt);
                    }

                    return response;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    attempt++;

                    if (attempt > retryCount)
                    {
                        _logger.LogError(
                            ex,
                            "[Tracking:{TraceId}] {CommandName:l} - All retry attempts exhausted ({MaxRetries}). Throwing exception.",
                            command.TraceId,
                            typeof(TCommand).Name,
                            retryCount);
                        throw;
                    }

                    var delay = attribute.GetDelayForIteration(attempt);
                    if (delay == 0)
                    {
                        delay = _options.Value.GetDefaultDelayForIteration(attempt);
                    }

                    _logger.LogWarning(
                        ex,
                        "[Tracking:{TraceId}] {CommandName:l} - Attempt {Attempt} failed. Retrying in {Delay}ms...",
                        command.TraceId,
                        typeof(TCommand).Name,
                        attempt,
                        delay);

                    if (delay > 0)
                    {
                        await Task.Delay(delay, cancellationToken);
                    }
                }
            }

            // This should never be reached, but just in case
            throw lastException ?? new InvalidOperationException("Retry logic failed unexpectedly");
        }
    }
}

