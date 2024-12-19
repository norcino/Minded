using AnonymousData;
using Minded.Extensions.Caching.Decorator;
using Minded.Extensions.Caching.Memory.Decorator;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.Caching.Abstractions.Tests
{
    [MemoryCache]
    public class TestQueryWithCachingAttribute : IQuery<string>, IGenerateCacheKey
    {
        public Guid TraceId => Any.Guid();

        /// <summary>
        /// NOTE: Irrelevan for the test but must be unique when used in production
        /// </summary>
        /// <returns></returns>
        public string GetCacheKey() => Any.String();
    }
}
