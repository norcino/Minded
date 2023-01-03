﻿using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Minded.Extensions.Decorator;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.Exception.Decorator
{
    public class ExceptionCommandHandlerDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand>
        where TCommand : ICommand
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
}