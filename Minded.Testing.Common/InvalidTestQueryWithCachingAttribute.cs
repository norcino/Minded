using AnonymousData;
using Minded.Extensions.Caching.Memory.Decorator;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.Caching.Abstractions.Tests
{
    [MemoryCache]
    public class InvalidTestQueryWithCachingAttribute : IQuery<string> // DO NOT IMPLEMENT IGenerateCacheKey
    {
        public Guid TraceId => Any.Guid();
    }
}
