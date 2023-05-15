namespace Minded.Extensions.Caching.Decorator
{
    /// <summary>
    /// This interface can be used to generate a custom cache key
    /// The key must be unique.
    /// </summary>
    public interface IGenerateCacheKey
    {
        /// <summary>
        /// Returns the unique cache key
        /// </summary>
        /// <returns>String cache key</returns>
        string GetCacheKey();
    }
}
