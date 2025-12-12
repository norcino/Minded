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
                string commandInfo;

                // Check if serialization is enabled
                if (_options.Value.GetEffectiveSerialize())
                {
                    commandInfo = "Command serialization unavailable";

                    try
                    {
                        // Use the centralized sanitization pipeline
                        // This applies all registered sanitizers (diagnostic, data protection, property exclusions, etc.)
                        IDictionary<string, object> sanitizedCommand = _sanitizerPipeline.Sanitize(command);
                        commandInfo = JsonSerializer.Serialize(sanitizedCommand);
                    }
                    catch { }
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
                string commandInfo;

                // Check if serialization is enabled
                if (_options.Value.GetEffectiveSerialize())
                {
                    commandInfo = "Command serialization unavailable";

                    try
                    {
                        // Use the centralized sanitization pipeline
                        // This applies all registered sanitizers (diagnostic, data protection, property exclusions, etc.)
                        IDictionary<string, object> sanitizedCommand = _sanitizerPipeline.Sanitize(command);
                        commandInfo = JsonSerializer.Serialize(sanitizedCommand);
                    }
                    catch { }
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
