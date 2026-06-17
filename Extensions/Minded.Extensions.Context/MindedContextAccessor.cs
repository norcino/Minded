using System.Threading;

namespace Minded.Extensions.Context
{
    /// <summary>
    /// Default <see cref="IMindedContextAccessor"/> backed by an <see cref="AsyncLocal{T}"/> slot.
    /// Registered as a singleton so that the same instance is shared across the application while the
    /// ambient value flows independently per logical async call.
    /// </summary>
    public sealed class MindedContextAccessor : IMindedContextAccessor
    {
        private readonly AsyncLocal<MindedContext> _current = new AsyncLocal<MindedContext>();

        /// <inheritdoc />
        public IMindedContext Current => _current.Value ?? (IMindedContext)NullMindedContext.Instance;

        /// <summary>
        /// Mutable access to the ambient context used by the context decorators to publish and restore
        /// the current value on entry and exit of the outermost mediator call.
        /// </summary>
        internal MindedContext InternalCurrent
        {
            get => _current.Value;
            set => _current.Value = value;
        }
    }
}
