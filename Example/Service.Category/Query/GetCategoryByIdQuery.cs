using System;
using Minded.Extensions.Caching.Decorator;
using Minded.Extensions.Caching.Memory.Decorator;
using Minded.Extensions.Logging;

namespace Service.Category.Query
{
    [MemoryCache(ExpirationInSeconds = 300)]
    public class GetCategoryByIdQuery : ILoggableQuery<Data.Entity.Category>, IGenerateCacheKey
    {
        public int CategoryId { get; }
        public Guid TraceId { get; } = Guid.NewGuid();

        public GetCategoryByIdQuery(int categoryId, Guid? traceId = null)
        {
            CategoryId = categoryId;
            TraceId = traceId ?? TraceId;
        }

        public LogData ToLog() => new(TraceId, "CategoryId: {CategoryId}", CategoryId);
        public string GetCacheKey() => $"{CategoryId}";
    }
}
