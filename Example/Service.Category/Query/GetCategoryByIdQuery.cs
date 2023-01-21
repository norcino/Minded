using System;
using Minded.Extensions.Logging;

namespace Service.Category.Query
{
    public class GetCategoryByIdQuery : ILoggableQuery<Data.Entity.Category>
    {
        public int CategoryId { get; }
        public Guid TraceId { get; } = Guid.NewGuid();

        public GetCategoryByIdQuery(int categoryId, Guid? traceId = null)
        {
            CategoryId = categoryId;
            TraceId = traceId ?? TraceId;
        }

        public LogData ToLog() => new(TraceId, "CategoryId: {CategoryId}", CategoryId);
    }
}
