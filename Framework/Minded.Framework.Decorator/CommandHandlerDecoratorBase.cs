using Minded.Framework.CQRS.Command;

namespace Minded.Framework.Decorator
{
    public abstract class CommandHandlerDecoratorBase<TCommand> where TCommand : ICommand
    {
        public ICommandHandler<TCommand> InnerCommandHandler => DecoratedCommmandHandler;
        protected readonly ICommandHandler<TCommand> DecoratedCommmandHandler;

        protected CommandHandlerDecoratorBase(ICommandHandler<TCommand> commandHandler)
        {
            DecoratedCommmandHandler = commandHandler;
        }
    }

    public abstract class CommandHandlerDecoratorBase<TCommand, TResult> where TCommand : ICommand<TResult>
    {
        public ICommandHandler<TCommand, TResult> InnerCommandHandler => DecoratedCommmandHandler;
        protected readonly ICommandHandler<TCommand, TResult> DecoratedCommmandHandler;

        protected CommandHandlerDecoratorBase(ICommandHandler<TCommand, TResult> commandHandler)
        {
            DecoratedCommmandHandler = commandHandler;
        }
    }
}
