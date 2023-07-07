using System;

namespace Minded.Framework.CQRS.Abstractions
{
    /// <summary>
    /// Interface used to define command and query shared properties
    /// </summary>
    public interface IMessage
    {

        /// <summary>
        /// Tracing Id used to track all command and queries coming from the same request
        /// </summary>
        Guid TraceId { get; }
    }
}
