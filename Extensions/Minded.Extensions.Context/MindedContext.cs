using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Minded.Framework.Mediator;

namespace Minded.Extensions.Context
{
    /// <summary>
    /// Default concrete <see cref="IMindedContext"/> created by the command and query context decorators.
    /// Not intended to be constructed directly by application code.
    /// </summary>
    public sealed class MindedContext : IMindedContext
    {
        private readonly ConcurrentDictionary<string, object> _items;
        private readonly ConcurrentDictionary<Type, object> _typedSlots;
        private readonly AsyncLocal<ImmutableDictionary<Type, ImmutableStack<object>>> _scopes;
        private int _depth;
        private int _disposed;

        /// <inheritdoc />
        public Guid TraceId { get; }

        /// <inheritdoc />
        public DateTimeOffset CreatedAtUtc { get; }

        /// <inheritdoc />
        public int Depth => Volatile.Read(ref _depth);

        /// <inheritdoc />
        public bool IsRoot => Volatile.Read(ref _depth) <= 1;

        /// <inheritdoc />
        public CancellationToken RootCancellationToken { get; }

        /// <inheritdoc />
        public IMediator Mediator { get; }

        /// <inheritdoc />
        public IDictionary<string, object> Items => _items;

        /// <summary>
        /// Creates a new context. Typically invoked by the context decorators on the outermost mediator
        /// call. The initial <see cref="Depth"/> is <c>1</c> representing the root call itself.
        /// </summary>
        /// <param name="traceId">Correlation id to propagate to nested <see cref="ITraceable"/> messages.</param>
        /// <param name="createdAtUtc">Creation timestamp captured at the outermost entry.</param>
        /// <param name="rootCancellationToken">Cancellation token received at the outermost entry.</param>
        /// <param name="mediator">Mediator that originated this context.</param>
        public MindedContext(Guid traceId, DateTimeOffset createdAtUtc, CancellationToken rootCancellationToken, IMediator mediator)
        {
            TraceId = traceId;
            CreatedAtUtc = createdAtUtc;
            RootCancellationToken = rootCancellationToken;
            Mediator = mediator;
            _items = new ConcurrentDictionary<string, object>();
            _typedSlots = new ConcurrentDictionary<Type, object>();
            _scopes = new AsyncLocal<ImmutableDictionary<Type, ImmutableStack<object>>>();
            _depth = 1;
        }

        /// <summary>
        /// Atomically increments the depth counter. Invoked by the context decorator when it detects a
        /// nested mediator call.
        /// </summary>
        /// <returns>The new depth value.</returns>
        internal int IncrementDepth() => Interlocked.Increment(ref _depth);

        /// <summary>
        /// Atomically decrements the depth counter. Invoked by the context decorator when a nested
        /// mediator call completes.
        /// </summary>
        /// <returns>The new depth value.</returns>
        internal int DecrementDepth() => Interlocked.Decrement(ref _depth);

        /// <inheritdoc />
        public void Set<T>(T value) => _typedSlots[typeof(T)] = value;

        /// <inheritdoc />
        public T Get<T>() => _typedSlots.TryGetValue(typeof(T), out var value) ? (T)value : default;

        /// <inheritdoc />
        public bool TryGet<T>(out T value)
        {
            if (_typedSlots.TryGetValue(typeof(T), out var stored))
            {
                value = (T)stored;
                return true;
            }
            value = default;
            return false;
        }

        /// <inheritdoc />
        public T GetOrAdd<T>(Func<T> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            return (T)_typedSlots.GetOrAdd(typeof(T), _ => factory());
        }

        /// <inheritdoc />
        public void Remove<T>() => _typedSlots.TryRemove(typeof(T), out _);

        /// <inheritdoc />
        public IDisposable BeginScope<T>(T value)
        {
            var previous = _scopes.Value ?? ImmutableDictionary<Type, ImmutableStack<object>>.Empty;
            var stack = previous.TryGetValue(typeof(T), out var existing) ? existing : ImmutableStack<object>.Empty;
            _scopes.Value = previous.SetItem(typeof(T), stack.Push(value));
            return new ScopeHandle(this, previous);
        }

        /// <inheritdoc />
        public bool TryGetScoped<T>(out T value)
        {
            var current = _scopes.Value;
            if (current != null && current.TryGetValue(typeof(T), out var stack) && !stack.IsEmpty)
            {
                value = (T)stack.Peek();
                return true;
            }
            value = default;
            return false;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            _items.Clear();
            _typedSlots.Clear();
            _scopes.Value = null;
        }

        /// <summary>
        /// Disposable returned by <see cref="BeginScope{T}(T)"/>. Restores the snapshot of the scoped
        /// state captured at the moment the scope was opened. Idempotent.
        /// </summary>
        private sealed class ScopeHandle : IDisposable
        {
            private readonly MindedContext _owner;
            private readonly ImmutableDictionary<Type, ImmutableStack<object>> _snapshot;
            private int _disposed;

            public ScopeHandle(MindedContext owner, ImmutableDictionary<Type, ImmutableStack<object>> snapshot)
            {
                _owner = owner;
                _snapshot = snapshot;
            }

            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                    return;
                _owner._scopes.Value = _snapshot;
            }
        }
    }
}
