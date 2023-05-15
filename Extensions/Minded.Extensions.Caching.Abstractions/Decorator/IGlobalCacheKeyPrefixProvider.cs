namespace Minded.Extensions.Caching.Decorator
{
    /// <summary>
    /// Interface to be implemente to generate globally valid prefix to guardantee uniqueness of the keys.
    /// A commpon example is to provide the tenant id in a multitenant system.
    /// </summary>
    public interface IGlobalCacheKeyPrefixProvider
    {
        /// <summary>
        /// Gets the global cache key prefix
        /// </summary>
        /// <returns>Cache key string prefix</returns>
        string GetGlobalCacheKeyPrefix();
    }
}
