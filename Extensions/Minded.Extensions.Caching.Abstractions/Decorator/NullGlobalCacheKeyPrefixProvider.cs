using Minded.Extensions.Caching.Decorator;

namespace Minded.Extensions.Caching.Abstractions.Decorator
{
    public class NullGlobalCacheKeyPrefixProvider : IGlobalCacheKeyPrefixProvider
    {
        public string GetGlobalCacheKeyPrefix() => "";
    }
}
