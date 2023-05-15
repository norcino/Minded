using System;

namespace Minded.Extensions.Caching.Abstractions.Decorator
{
    /// <summary>
    /// Caches the query result using the given cofiguration
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public abstract class CacheAttribute : Attribute
    {
        /// <summary>
        /// Seconds from now when the cache entry will be evicted
        /// </summary>
        public int ExpirationInSeconds { get; set; }

        /// <summary>
        /// Seconds from last access when the cache entry will be evicted
        /// This will not extend the entry lifetime beyond the absolute expiration (if set)
        /// </summary>
        public int SlidingExpiration { get; set; }

        ///// <summary>
        ///// Date and time when the cache entry will be evicted
        ///// </summary>
        public DateTimeOffset AbsoluteExpiration { get; set; }
    }
}
