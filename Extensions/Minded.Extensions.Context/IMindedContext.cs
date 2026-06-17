using System;
using System.Collections.Generic;
using System.Threading;
using Minded.Framework.Mediator;

namespace Minded.Extensions.Context
{
    /// <summary>
    /// Ambient execution context shared by all decorators and handlers participating in the processing
    /// of a single outermost mediator invocation and any nested mediator calls originated within it.
    /// The instance is created by the context decorator on the first mediator entry and disposed when
    /// that outermost call completes. Nested calls see the same instance.
    /// </summary>
    /// <remarks>
    /// The context is exposed to application code through <see cref="IMindedContextAccessor.Current"/>.
    /// All mutable members are thread-safe so that handlers spawning parallel sub-invocations can write
    /// concurrently without additional synchronization.
    /// </remarks>
    public interface IMindedContext : IDisposable
    {
        /// <summary>
        /// Correlation identifier shared across the root invocation and all nested mediator calls.
        /// Seeded from the root command or query when it implements <see cref="ITraceable"/>.
        /// </summary>
        Guid TraceId { get; }

        /// <summary>
        /// UTC timestamp taken when the context was created at the outermost mediator entry.
        /// </summary>
        DateTimeOffset CreatedAtUtc { get; }

        /// <summary>
        /// Current nesting level of the mediator call stack. The outermost call reports <c>1</c>,
        /// each nested call increments the value while it is being processed.
        /// </summary>
        int Depth { get; }

        /// <summary>
        /// Indicates whether the current mediator call is the outermost one for this context.
        /// </summary>
        bool IsRoot { get; }

        /// <summary>
        /// Cancellation token passed to the outermost mediator invocation. Exposed as a convenience for
        /// deeply nested code that has access to <see cref="IMindedContext"/> but not the per-call token.
        /// </summary>
        CancellationToken RootCancellationToken { get; }

        /// <summary>
        /// Mediator instance that originated this context. Allows handlers and decorators that already
        /// depend on <see cref="IMindedContext"/> to dispatch nested commands and queries without also
        /// taking a direct dependency on <see cref="IMediator"/>.
        /// </summary>
        IMediator Mediator { get; }

        /// <summary>
        /// Thread-safe string-keyed property bag for ad-hoc metadata exchange between decorators and
        /// handlers within the same context. The context does not take ownership of stored values and
        /// does not dispose them.
        /// </summary>
        IDictionary<string, object> Items { get; }

        /// <summary>
        /// Stores a strongly typed value keyed by its runtime type. Overwrites any previous value for
        /// the same type.
        /// </summary>
        /// <typeparam name="T">Type used as key and as storage type for the value.</typeparam>
        /// <param name="value">Value to associate with the type key.</param>
        void Set<T>(T value);

        /// <summary>
        /// Retrieves a strongly typed value previously stored via <see cref="Set{T}(T)"/> or
        /// <see cref="GetOrAdd{T}(Func{T})"/>. Returns the default value if no entry exists.
        /// </summary>
        /// <typeparam name="T">Type used as key and as storage type for the value.</typeparam>
        T Get<T>();

        /// <summary>
        /// Attempts to retrieve a strongly typed value without creating one if it is missing.
        /// </summary>
        /// <typeparam name="T">Type used as key and as storage type for the value.</typeparam>
        /// <param name="value">Retrieved value when present, default otherwise.</param>
        /// <returns><c>true</c> when a value was found, <c>false</c> otherwise.</returns>
        bool TryGet<T>(out T value);

        /// <summary>
        /// Returns the existing strongly typed value or creates and stores a new one atomically.
        /// </summary>
        /// <typeparam name="T">Type used as key and as storage type for the value.</typeparam>
        /// <param name="factory">Factory invoked when no value is present.</param>
        T GetOrAdd<T>(Func<T> factory);

        /// <summary>
        /// Removes the strongly typed entry associated with <typeparamref name="T"/>, if any.
        /// </summary>
        /// <typeparam name="T">Type used as key and as storage type for the value.</typeparam>
        void Remove<T>();

        /// <summary>
        /// Pushes a value onto an ambient, async-flow scoped stack keyed by <typeparamref name="T"/>.
        /// The value is visible to code running within the current logical call, including continuations
        /// and nested mediator dispatches, but not to sibling branches running in parallel that forked
        /// before the scope was opened. Disposing the returned handle pops the value and restores the
        /// previous state observable from the disposing flow.
        /// </summary>
        /// <remarks>
        /// Designed for flow-local flags such as "bypass this decorator for the sub-invocation I am about
        /// to dispatch" where the <see cref="Items"/> property bag and <see cref="Set{T}(T)"/> typed slots
        /// are unsuitable because they are shared across all concurrent branches of the same context.
        /// Scopes must be disposed in LIFO order within the same async flow that opened them.
        /// </remarks>
        /// <typeparam name="T">Type used as key for the scoped stack and as the stored value type.</typeparam>
        /// <param name="value">Value to push onto the scoped stack.</param>
        /// <returns>Handle that pops the scope when disposed. Safe to dispose more than once.</returns>
        IDisposable BeginScope<T>(T value);

        /// <summary>
        /// Reads the top value of the ambient scoped stack for <typeparamref name="T"/> if one is active
        /// on the current async flow.
        /// </summary>
        /// <typeparam name="T">Type used as key for the scoped stack.</typeparam>
        /// <param name="value">Top value of the stack when present, default otherwise.</param>
        /// <returns><c>true</c> when at least one scope is active for <typeparamref name="T"/>, otherwise <c>false</c>.</returns>
        bool TryGetScoped<T>(out T value);
    }
}
