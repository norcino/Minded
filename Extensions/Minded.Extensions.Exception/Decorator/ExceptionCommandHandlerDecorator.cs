using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minded.Extensions.DataProtection.Abstractions;
using Minded.Extensions.Exception.Configuration;
using Minded.Framework.CQRS.Command;
using Minded.Framework.Decorator;
using Minded.Framework.CQRS.Abstractions.Sanitization;

namespace Minded.Extensions.Exception.Decorator
{
    /// <summary>
    /// Lazy wrapper for JSON serialization that only serializes when ToString() is called.
    /// This avoids unnecessary serialization overhead when the value is not actually used.
    /// Performance: Saves 100% of serialization cost when logging is disabled or log level is not enabled.
    /// </summary>
    internal class LazyJsonValue
    {
        private readonly IDictionary<string, object> _value;
        private string _serialized;

        public LazyJsonValue(IDictionary<string, object> value)
        {
            _value = value;
        }

        public override string ToString()
        {
            if (_serialized == null && _value != null)
            {
                try
                {
                    _serialized = JsonSerializer.Serialize(_value);
                }
                catch
                {
                    _serialized = "Serialization failed";
                }
            }
            return _serialized ?? "null";
        }
    }
    public class ExceptionCommandHandlerDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand> where TCommand : ICommand
    {
        private readonly ILogger<ExceptionCommandHandlerDecorator<TCommand>> _logger;
        private readonly ILoggingSanitizerPipeline _sanitizerPipeline;
        private readonly IOptions<ExceptionOptions> _options;

        public ExceptionCommandHandlerDecorator(
            ICommandHandler<TCommand> commandHandler,
            ILogger<ExceptionCommandHandlerDecorator<TCommand>> logger,
            ILoggingSanitizerPipeline sanitizerPipeline,
            IOptions<ExceptionOptions> options) : base(commandHandler)
        {
            _logger = logger;
            _sanitizerPipeline = sanitizerPipeline;
            _options = options;
        }

        public async Task<ICommandResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Request was cancelled (client disconnect, timeout, etc.)
                // This is not an error - just log as information and re-throw
                _logger.LogInformation("Command {CommandType} was cancelled", typeof(TCommand).Name);
                throw; // Re-throw to let ASP.NET Core or RestMediator handle it
            }
            catch (System.Exception ex)
            {
                object commandInfo;

                // Check if serialization is enabled
                if (_options.Value.GetEffectiveSerialize())
                {
                    try
                    {
                        // Use the centralized sanitization pipeline
                        // This applies all registered sanitizers (diagnostic, data protection, property exclusions, etc.)
                        IDictionary<string, object> sanitizedCommand = _sanitizerPipeline.Sanitize(command);

                        // Use lazy serialization - only serializes when ToString() is called
                        // This avoids serialization overhead if the exception message is not logged
                        commandInfo = new LazyJsonValue(sanitizedCommand);
                    }
                    catch
                    {
                        commandInfo = "Command serialization unavailable";
                    }
                }
                else
                {
                    // Serialization disabled - just include the command type name
                    commandInfo = $"Type: {typeof(TCommand).Name} (serialization disabled)";
                }

                _logger.LogError(ex, ex.Message);

                throw new CommandHandlerException<TCommand>(command, "CommandHandlerException: " + commandInfo, ex);
            }
        }
    }

    public class ExceptionCommandHandlerDecorator<TCommand, TResult> : CommandHandlerDecoratorBase<TCommand, TResult>, ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
    {
        private readonly ILogger _logger;
        private readonly ILoggingSanitizerPipeline _sanitizerPipeline;
        private readonly IOptions<ExceptionOptions> _options;

        public ExceptionCommandHandlerDecorator(
            ICommandHandler<TCommand, TResult> commandHandler,
            ILogger<ExceptionCommandHandlerDecorator<TCommand, TResult>> logger,
            ILoggingSanitizerPipeline sanitizerPipeline,
            IOptions<ExceptionOptions> options) : base(commandHandler)
        {
            _logger = logger;
            _sanitizerPipeline = sanitizerPipeline;
            _options = options;
        }

        public async Task<ICommandResponse<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Request was cancelled (client disconnect, timeout, etc.)
                // This is not an error - just log as information and re-throw
                _logger.LogInformation("Command {CommandType} was cancelled", typeof(TCommand).Name);
                throw; // Re-throw to let ASP.NET Core or RestMediator handle it
            }
            catch (System.Exception ex)
            {
                object commandInfo;

                // Check if serialization is enabled
                if (_options.Value.GetEffectiveSerialize())
                {
                    try
                    {
                        // Use the centralized sanitization pipeline
                        // This applies all registered sanitizers (diagnostic, data protection, property exclusions, etc.)
                        IDictionary<string, object> sanitizedCommand = _sanitizerPipeline.Sanitize(command);

                        // Use lazy serialization - only serializes when ToString() is called
                        // This avoids serialization overhead if the exception message is not logged
                        commandInfo = new LazyJsonValue(sanitizedCommand);
                    }
                    catch
                    {
                        commandInfo = "Command serialization unavailable";
                    }
                }
                else
                {
                    // Serialization disabled - just include the command type name
                    commandInfo = $"Type: {typeof(TCommand).Name} (serialization disabled)";
                }

                _logger.LogError(ex, ex.Message);

                throw new CommandHandlerException<TCommand>(command, "CommandHandlerException: " + commandInfo, ex);
            }
        }
    }
}
