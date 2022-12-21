using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.Decorator
{
    public abstract class CommandHandlerDecoratorBase<TCommand>
        where TCommand : ICommand
    {
        public ICommandHandler<TCommand> InnerCommandHandler => DecoratedCommmandHandler;
        protected readonly ICommandHandler<TCommand> DecoratedCommmandHandler;

        protected CommandHandlerDecoratorBase(ICommandHandler<TCommand> commandHandler)
        {
            DecoratedCommmandHandler = commandHandler;
        }
    }
}
