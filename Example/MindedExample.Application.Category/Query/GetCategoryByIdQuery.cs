using System;
using Minded.Extensions.Caching.Decorator;
using Minded.Extensions.Caching.Memory.Decorator;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;

namespace MindedExample.Application.Category.Query
{
    [MemoryCache(ExpirationInSeconds = 300)]
    public class GetCategoryByIdQuery : IQuery<MindedExample.Domain.Category>, IGenerateCacheKey, ILoggable
    {
        public int CategoryId { get; }

        public GetCategoryByIdQuery(int categoryId, Guid? traceId = null)
        {
            CategoryId = categoryId;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "CategoryId: {CategoryId}";

        public string[] LoggingProperties => [nameof(CategoryId)];

        public string GetCacheKey() => $"CategoryId-{CategoryId}";
    }
}