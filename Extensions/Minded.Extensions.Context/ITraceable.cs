using System;

namespace Minded.Extensions.Context
{
    /// <summary>
    /// Opt-in marker for commands and queries whose <see cref="TraceId"/> should be aligned with the
    /// ambient <see cref="IMindedContext.TraceId"/> when they are dispatched as nested calls through
    /// the mediator. Commands and queries that do not implement this interface retain their own
    /// independently generated <c>TraceId</c> and are unaffected by the context extension.
    /// </summary>
    /// <remarks>
    /// When the outermost mediator call creates a new <see cref="IMindedContext"/>, the context's
    /// <c>TraceId</c> is seeded from the incoming command or query if it implements <see cref="ITraceable"/>,
    /// otherwise a new <see cref="Guid"/> is generated. For every nested mediator call performed within
    /// the same context, the context decorator overwrites the nested command or query <c>TraceId</c>
    /// with the ambient value so that all log entries share the same correlation id without requiring
    /// the developer to propagate it manually.
    /// </remarks>
    public interface ITraceable
    {
        /// <summary>
        /// Correlation identifier for the command or query. Must be settable so that the context
        /// decorator can align nested invocations with the ambient trace id.
        /// </summary>
        Guid TraceId { get; set; }
    }
}
