using Minded.Extensions.Caching.Decorator;

namespace Minded.Extensions.Caching.Abstractions.Decorator
{
    public class EmptyGlobalCacheKeyPrefixProvider : IGlobalCacheKeyPrefixProvider
    {
        public string GetGlobalCacheKeyPrefix() => "";
    }
}
