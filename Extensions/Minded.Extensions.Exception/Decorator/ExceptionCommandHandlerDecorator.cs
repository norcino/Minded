using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Minded.Framework.CQRS.Command;
using Minded.Framework.Decorator;

namespace Minded.Extensions.Exception.Decorator
{
    public class ExceptionCommandHandlerDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand> where TCommand : ICommand
    {
        private readonly ILogger<ExceptionCommandHandlerDecorator<TCommand>> _logger;

        public ExceptionCommandHandlerDecorator(ICommandHandler<TCommand> commandHandler, ILogger<ExceptionCommandHandlerDecorator<TCommand>> logger) : base(commandHandler)
        {
            _logger = logger;
        }

        public async Task<ICommandResponse> HandleAsync(TCommand command)
        {
            try
            {
                return await DecoratedCommmandHandler.HandleAsync(command);
            }
            catch (System.Exception ex)
            {
                var commandJson = "Command serialization unavailable";

                try
                {
                    commandJson = JsonSerializer.Serialize(command);
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

        public ExceptionCommandHandlerDecorator(ICommandHandler<TCommand, TResult> commandHandler, ILogger<ExceptionCommandHandlerDecorator<TCommand, TResult>> logger) : base(commandHandler)
        {
            _logger = logger;
        }

        public async Task<ICommandResponse<TResult>> HandleAsync(TCommand command)
        {
            try
            {
                return await DecoratedCommmandHandler.HandleAsync(command);
            }
            catch (System.Exception ex)
            {
                var commandJson = "Command serialization unavailable";

                try
                {
                    commandJson = JsonSerializer.Serialize(command);
                }
                catch { }

                _logger.LogError(ex, ex.Message);

                throw new CommandHandlerException<TCommand>(command, "CommandHandlerException: " + commandJson, ex);
            }
        }
    }
}
