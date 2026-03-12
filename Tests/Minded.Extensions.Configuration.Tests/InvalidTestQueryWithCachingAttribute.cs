using AnonymousData;
using Minded.Extensions.Caching.Memory.Decorator;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.Configuration.Tests
{
    [MemoryCache]
    internal class InvalidTestQueryWithCachingAttribute : IQuery<string> // DO NOT IMPLEMENT IGenerateCacheKey
    {
        public Guid TraceId => Any.Guid();
    }
}
