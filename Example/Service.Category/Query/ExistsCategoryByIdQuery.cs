using System;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;

namespace Service.Category.Query
{
 /// <summary>
 /// Query that returns true if the Category with the specified Id exists, false otherwise.
 /// Lightweight (no caching) to avoid stale existence checks.
 /// </summary>
 public class ExistsCategoryByIdQuery : IQuery<bool>, ILoggable
 {
 public int CategoryId { get; }

 public ExistsCategoryByIdQuery(int categoryId, Guid? traceId = null)
 {
 CategoryId = categoryId;
 TraceId = traceId ?? TraceId;
 }

 public Guid TraceId { get; } = Guid.NewGuid();

 public string LoggingTemplate => "CategoryId: {CategoryId}";

 public object[] LoggingParameters => new object[] { CategoryId };
 }
}
