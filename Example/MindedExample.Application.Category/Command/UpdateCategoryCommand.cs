using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Extensions.Authorization.Attributes;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.Category.Command
{
    /// <summary>
    /// Command to update an existing category.
    /// Implements ILoggable for automatic logging through the logging decorator.
    /// Uses ValidateCommand attribute to ensure the category exists before update.
    /// </summary>
    [ValidateCommand]
    [RequirePermissions(Domain.Permissions.CanUpdateCategory)]
    [RequireClaim("is_global_admin", "false")]
    public class UpdateCategoryCommand : ICommand, ILoggable
    {
        public MindedExample.Domain.Category Category { get; }
        public int CategoryId { get; }

        public UpdateCategoryCommand(int id, MindedExample.Domain.Category category, Guid? traceId = null)
        {
            CategoryId = id;
            Category = category;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "CategoryId: {CategoryId} CategoryName: {CategoryName}";

        public string[] LoggingProperties => ["Category.Id", "Category.Name"];
    }
}