using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.Logging
{
    /// <summary>
    /// Interface of a command which can be supports ILoggable trait
    /// </summary>
    public interface ILoggableCommand : ICommand, ILoggable
    {
    }

    /// <summary>
    /// Interface of a command which can be supports ILoggable trait
    /// </summary>
    /// <typeparam name="TResult">Command result Type</typeparam>
    public interface ILoggableCommand<TResult> : ICommand<TResult>, ILoggable
    {
    }
}
