using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Extensions.Retry.Decorator;
using Minded.Framework.CQRS.Command;

namespace Service.Category.Command
{
    /// <summary>
    /// Command to create a new category.
    /// This command is validated before execution and includes retry logic.
    /// The retry decorator will retry up to 3 times with increasing delays (100ms, 200ms, 400ms).
    /// </summary>
    [ValidateCommand]
    [RetryCommand(3, 100, 200, 400)]
    public class CreateCategoryCommand : ICommand<Data.Entity.Category>, ILoggable
    {
        public Data.Entity.Category Category { get; set; }

        public CreateCategoryCommand(Data.Entity.Category category, Guid? traceId = null)
        {
            Category = category;
            TraceId = traceId ?? TraceId;
        }
        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "CategoryName {CategoryName}";

        public object[] LoggingParameters => new object[] { Category.Name };
    }
}
