using System;

namespace Minded.Extensions.Caching.Abstractions.Decorator
{
    /// <summary>
    /// Caches the query result using the given configuration
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public abstract class CacheAttribute : Attribute
    {
        /// <summary>
        /// Seconds from now when the cache entry will be evicted
        /// </summary>
        public int ExpirationInSeconds { get; set; }

        /// <summary>
        /// Seconds from last access when the cache entry will be evicted (sliding expiration).
        /// Each access renews the entry lifetime by this duration.
        /// This will not extend the entry lifetime beyond the absolute expiration (if set)
        /// </summary>
        public int SlidingExpiration { get; set; }

        /// <summary>
        /// Date and time when the cache entry will be evicted.
        /// The accepted format is ISO 8601 representation 'e.g. 2023-06-30T12:00:00Z'.
        /// </summary>
        public string AbsoluteExpiration { get; set; }

        /// <summary>
        /// Errors are caught and suppressed by default, if caching is mandatory set this
        /// property to break the application flow and throw the exception
        /// </summary>
        public bool FailOnError{ get; set; }
    }
}