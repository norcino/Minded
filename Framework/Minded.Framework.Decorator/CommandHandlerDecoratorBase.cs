using Minded.Framework.CQRS.Command;

namespace Minded.Framework.Decorator
{
    /// <summary>
    /// Base class for command handler decorators without result.
    /// Provides access to the decorated command handler instance.
    /// </summary>
    /// <typeparam name="TCommand">Type of command being handled</typeparam>
    public abstract class CommandHandlerDecoratorBase<TCommand> where TCommand : ICommand
    {
        public ICommandHandler<TCommand> InnerCommandHandler => DecoratedCommmandHandler;
        protected readonly ICommandHandler<TCommand> DecoratedCommmandHandler;

        protected CommandHandlerDecoratorBase(ICommandHandler<TCommand> commandHandler)
        {
            DecoratedCommmandHandler = commandHandler;
        }
    }

    /// <summary>
    /// Base class for command handler decorators with result.
    /// Provides access to the decorated command handler instance.
    /// </summary>
    /// <typeparam name="TCommand">Type of command being handled</typeparam>
    /// <typeparam name="TResult">Type of result returned by the command</typeparam>
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
