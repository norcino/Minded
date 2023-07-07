using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace Service.Category.Command
{
    [ValidateCommand]
    public class CreateCategoryCommand : ICommand<Data.Entity.Category>, ILoggable
    {
        public Data.Entity.Category Category { get; set; }

        public CreateCategoryCommand(Data.Entity.Category category, Guid? traceId = null)
        {
            Category = category;
            TraceId = traceId ?? TraceId;
        }
        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "CategoryName {Name}";

        public object[] LoggingParameters => new object[] { Category.Name };
    }
}
