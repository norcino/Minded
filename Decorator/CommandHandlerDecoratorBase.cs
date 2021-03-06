﻿using Minded.Common;

namespace Minded.Decorator
{
    public abstract class CommandHandlerDecoratorBase<TCommand>
        where TCommand : ICommand
    {
        public ICommandHandler<TCommand> InnerCommandHandler => CommmandHandler;
        protected readonly ICommandHandler<TCommand> CommmandHandler;

        protected CommandHandlerDecoratorBase(ICommandHandler<TCommand> commandHandler)
        {
            CommmandHandler = commandHandler;
        }
    }
}