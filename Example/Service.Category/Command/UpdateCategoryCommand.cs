using System;
using Minded.Extensions.Logging;

namespace Service.Category.Command
{
    public class UpdateCategoryCommand : ILoggableCommand
    {
        public Data.Entity.Category Category { get; }
        public int CategoryId { get; }
        public Guid TraceId { get; } = Guid.NewGuid();

        public UpdateCategoryCommand(int id, Data.Entity.Category category, Guid? traceId = null)
        {
            CategoryId = id;
            Category = category;
            TraceId = traceId ?? TraceId;
        }

        public LogData ToLog() => new(TraceId, "CategoryId: {Id} Category: {Name}", Category.Id, Category.Name);
    }
}
