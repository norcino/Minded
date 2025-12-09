using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minded.Extensions.DataProtection.Abstractions;
using Minded.Framework.CQRS.Command;
using Minded.Framework.Decorator;

namespace Minded.Extensions.Exception.Decorator
{
    public class ExceptionCommandHandlerDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand> where TCommand : ICommand
    {
        private readonly ILogger<ExceptionCommandHandlerDecorator<TCommand>> _logger;
        private readonly IDataSanitizer _dataSanitizer;
        private readonly IOptions<DataProtectionOptions> _options;

        public ExceptionCommandHandlerDecorator(ICommandHandler<TCommand> commandHandler, ILogger<ExceptionCommandHandlerDecorator<TCommand>> logger, IDataSanitizer dataSanitizer, IOptions<DataProtectionOptions> options) : base(commandHandler)
        {
            _logger = logger;
            _dataSanitizer = dataSanitizer;
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
                var commandJson = "Command serialization unavailable";

                try
                {
                    // Sanitize command before serialization to protect sensitive data
                    var sanitizedCommand = _dataSanitizer.Sanitize(command);
                    commandJson = JsonSerializer.Serialize(sanitizedCommand);
                }
                catch { }

                _logger.LogError(ex, ex.Message);

                throw new CommandHandlerException<TCommand>(command, "CommandHandlerException: " + commandJson, ex);
            }
        }
    }

    public class ExceptionCommandHandlerDecorator<TCommand, TResult> : CommandHandlerDecoratorBase<TCommand, TResult>, ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
    {
        private readonly ILogger _logger;
        private readonly IDataSanitizer _dataSanitizer;
        private readonly IOptions<DataProtectionOptions> _options;

        public ExceptionCommandHandlerDecorator(ICommandHandler<TCommand, TResult> commandHandler, ILogger<ExceptionCommandHandlerDecorator<TCommand, TResult>> logger, IDataSanitizer dataSanitizer, IOptions<DataProtectionOptions> options) : base(commandHandler)
        {
            _logger = logger;
            _dataSanitizer = dataSanitizer;
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
                var commandJson = "Command serialization unavailable";

                try
                {
                    // Sanitize command before serialization to protect sensitive data
                    var sanitizedCommand = _dataSanitizer.Sanitize(command);
                    commandJson = JsonSerializer.Serialize(sanitizedCommand);
                }
                catch { }

                _logger.LogError(ex, ex.Message);

                throw new CommandHandlerException<TCommand>(command, "CommandHandlerException: " + commandJson, ex);
            }
        }
    }
}
