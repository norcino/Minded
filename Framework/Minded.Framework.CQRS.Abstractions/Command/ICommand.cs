using System;

namespace Minded.Framework.CQRS.Command
{
    /// <summary>
    /// Base command interface
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Tracing Id used to track all command and queries coming from the same request
        /// </summary>
        Guid TraceId { get; }
    }

    /// <summary>
    /// Base command interface
    /// </summary>
    /// <typeparam name="TResult">Command result Type</typeparam>
    public interface ICommand<TResult> : ICommand
    {
    }
}
