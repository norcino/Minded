using System;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Command;

namespace Service.Category.Command
{
    public class UpdateCategoryCommand : ICommand, ILoggable
    {
        public Data.Entity.Category Category { get; }
        public int CategoryId { get; }

        public UpdateCategoryCommand(int id, Data.Entity.Category category, Guid? traceId = null)
        {
            CategoryId = id;
            Category = category;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "CategoryId: {Id} CategoryName: {Name}";

        public object[] LoggingParameters => new object[] { Category.Id, Category.Name };
    }
}
