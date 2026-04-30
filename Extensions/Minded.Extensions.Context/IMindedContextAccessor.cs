namespace Minded.Extensions.Context
{
    /// <summary>
    /// Ambient accessor for the current <see cref="IMindedContext"/>. The value flows across async
    /// boundaries via <see cref="System.Threading.AsyncLocal{T}"/>, including <c>Task.WhenAll</c> and
    /// <c>Task.Run</c>, and is reset automatically when the outermost mediator call completes.
    /// </summary>
    /// <remarks>
    /// The accessor is populated by the command and query context decorators. When the decorators are
    /// not registered, or when reads happen outside of a mediator invocation, <see cref="Current"/>
    /// returns a no-op <see cref="NullMindedContext"/> so callers never need to null-check the result.
    /// The accessor is registered as a singleton so it can be injected anywhere in the application.
    /// </remarks>
    public interface IMindedContextAccessor
    {
        /// <summary>
        /// Gets the ambient context for the current logical call, or <see cref="NullMindedContext.Instance"/>
        /// when no mediator invocation is in progress or the context decorators are not registered.
        /// </summary>
        IMindedContext Current { get; }
    }
}
