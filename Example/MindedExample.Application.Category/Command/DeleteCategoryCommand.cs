using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Extensions.Retry.Decorator;
using Minded.Extensions.Authorization.Attributes;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.Category.Command
{
    /// <summary>
    /// Command to delete a category by ID.
    /// Implements ILoggable for automatic logging through the logging decorator.
    /// Uses ValidateCommand attribute to ensure the category exists before deletion.
    /// Uses RetryCommand attribute as delete is idempotent and safe to retry on transient failures.
    /// </summary>
    [ValidateCommand]
    [RetryCommand]
    [RequirePermissions(Domain.Permissions.CanDeleteCategory)]
    [RequireClaim("is_global_admin", "false")]
    public class DeleteCategoryCommand : ICommand, ILoggable
    {
        public int CategoryId { get; }

        public DeleteCategoryCommand(int id, Guid? traceId = null)
        {
            CategoryId = id;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "CategoryId: {CategoryId}";

        public string[] LoggingProperties => [nameof(CategoryId)];
    }
}