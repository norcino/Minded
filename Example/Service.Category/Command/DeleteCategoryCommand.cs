using System;
using Minded.Extensions.Logging;

namespace Service.Category.Command
{
    public class DeleteCategoryCommand : ILoggableCommand
    {
        public int CategoryId { get; }
        public Guid TraceId { get; } = Guid.NewGuid();

        public DeleteCategoryCommand(int id, Guid? traceId = null)
        {
            CategoryId = id;
            TraceId = traceId ?? TraceId;
        }

        public LogData ToLog() => new(TraceId, "CategoryId: {Id}", CategoryId);
    }
}
