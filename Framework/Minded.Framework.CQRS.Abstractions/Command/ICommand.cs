using Minded.Framework.CQRS.Abstractions;

namespace Minded.Framework.CQRS.Command
{
    /// <summary>
    /// Base command interface
    /// </summary>
    public interface ICommand : IMessage
    {
    }

    /// <summary>
    /// Base command interface
    /// </summary>
    /// <typeparam name="TResult">Command result Type</typeparam>
    public interface ICommand<TResult> : ICommand
    {
    }
}
