using System;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Command;

namespace Service.Category.Command
{
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

        public object[] LoggingParameters => new object[] { CategoryId };
    }
}
