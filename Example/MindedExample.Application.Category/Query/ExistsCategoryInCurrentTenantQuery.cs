using System;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;

namespace MindedExample.Application.Category.Query
{
    /// <summary>
    /// Query that returns true if the Category with the specified Id exists in the current caller's tenant, false otherwise.
    /// Tenant scoping is enforced by the handler via <see cref="MindedExample.Infrastructure.Persistence.ICurrentUserAccessor"/>.
    /// Lightweight (no caching) to avoid stale existence checks.
    /// </summary>
    public class ExistsCategoryInCurrentTenantQuery : IQuery<bool>, ILoggable
    {
        public int CategoryId { get; }

        public ExistsCategoryInCurrentTenantQuery(object categoryId, string claimName)
        {
            CategoryId = (int)categoryId;
        }

        public ExistsCategoryInCurrentTenantQuery(object categoryId, Guid? traceId = null)
        {
            CategoryId = (int)categoryId;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "CategoryId: {CategoryId}";

        public string[] LoggingProperties => [nameof(CategoryId)];
    }
}
