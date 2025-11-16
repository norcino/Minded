using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace Service.Category.Command
{
    /// <summary>
    /// Command to update an existing category.
    /// Implements ILoggable for automatic logging through the logging decorator.
    /// Uses ValidateCommand attribute to ensure the category exists before update.
    /// </summary>
    [ValidateCommand]
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

        public string LoggingTemplate => "CategoryId: {CategoryId} CategoryName: {CategoryName}";

        public object[] LoggingParameters => new object[] { Category.Id, Category.Name };
    }
}
