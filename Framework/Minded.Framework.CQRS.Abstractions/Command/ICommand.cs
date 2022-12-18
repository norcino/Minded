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
    /// Command interface that returns a result object
    /// </summary>
    /// <typeparam name="TResult">Result type</typeparam>
    public interface ICommand<TResult> : ICommand
    {
        TResult Result { get; }
    }
}
