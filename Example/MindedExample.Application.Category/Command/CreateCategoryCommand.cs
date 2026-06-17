using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Extensions.Retry.Decorator;
using Minded.Extensions.Authorization.Attributes;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.Category.Command
{
    /// <summary>
    /// Command to create a new category.
    /// This command is validated before execution and includes retry logic.
    /// The retry decorator can be configured to retry up to 3 times with increasing delays (100ms, 200ms, 400ms).
    /// Using [RetryCommand(3, 100, 200, 400)], but this command will be configured to use default settings.
    /// </summary>
    [ValidateCommand]
    [RetryCommand]
    [RequirePermissions(Domain.Permissions.CanCreateCategory)]
    [RequireClaim("is_global_admin", "false")]
    public class CreateCategoryCommand : ICommand<MindedExample.Domain.Category>, ILoggable
    {
        public MindedExample.Domain.Category Category { get; set; }

        public CreateCategoryCommand(MindedExample.Domain.Category category, Guid? traceId = null)
        {
            Category = category;
            TraceId = traceId ?? TraceId;
        }
        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "CategoryName {CategoryName}";

        public string[] LoggingProperties => ["Category.Name"];
    }
}