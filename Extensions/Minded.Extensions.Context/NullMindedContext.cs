using System;
using System.Collections.Generic;
using System.Threading;
using Minded.Framework.Mediator;

namespace Minded.Extensions.Context
{
    /// <summary>
    /// No-op <see cref="IMindedContext"/> returned by <see cref="IMindedContextAccessor.Current"/> when
    /// no mediator invocation is in progress or the context decorators have not been registered. Avoids
    /// forcing consumers to guard against <c>null</c> when reading the ambient context.
    /// </summary>
    public sealed class NullMindedContext : IMindedContext
    {
        /// <summary>
        /// Shared singleton instance. The type is intentionally stateless.
        /// </summary>
        public static readonly NullMindedContext Instance = new NullMindedContext();

        private NullMindedContext() { }

        /// <inheritdoc />
        public Guid TraceId => Guid.Empty;

        /// <inheritdoc />
        public DateTimeOffset CreatedAtUtc => DateTimeOffset.MinValue;

        /// <inheritdoc />
        public int Depth => 0;

        /// <inheritdoc />
        public bool IsRoot => false;

        /// <inheritdoc />
        public CancellationToken RootCancellationToken => CancellationToken.None;

        /// <inheritdoc />
        public IMediator Mediator => null;

        /// <inheritdoc />
        public IDictionary<string, object> Items => EmptyDictionary;

        private static readonly IDictionary<string, object> EmptyDictionary = new Dictionary<string, object>(0);

        /// <inheritdoc />
        public void Set<T>(T value) { }

        /// <inheritdoc />
        public T Get<T>() => default;

        /// <inheritdoc />
        public bool TryGet<T>(out T value)
        {
            value = default;
            return false;
        }

        /// <inheritdoc />
        public T GetOrAdd<T>(Func<T> factory) => factory == null ? default : factory();

        /// <inheritdoc />
        public void Remove<T>() { }

        /// <inheritdoc />
        public IDisposable BeginScope<T>(T value) => NoopScope.Instance;

        /// <inheritdoc />
        public bool TryGetScoped<T>(out T value)
        {
            value = default;
            return false;
        }

        /// <inheritdoc />
        public void Dispose() { }

        private sealed class NoopScope : IDisposable
        {
            public static readonly NoopScope Instance = new NoopScope();
            private NoopScope() { }
            public void Dispose() { }
        }
    }
}
