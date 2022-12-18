using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;

namespace Service.Category.Command
{
    [ValidateCommand]
    public class CreateCategoryCommand : ILoggableCommand
    {
        public Data.Entity.Category Category { get; set; }
        public Guid TraceId { get; } = Guid.NewGuid();

        public CreateCategoryCommand(Data.Entity.Category category, Guid? traceId = null)
        {
            Category = category;
            TraceId = traceId ?? TraceId;
        }

        public LogData ToLog() => new(TraceId, "Category: {Name}", Category.Name);
    }
}
