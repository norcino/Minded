using System;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;

namespace MindedExample.Application.User.Query
{
    /// <summary>
    /// Query that returns true if the specified user exists in the current caller's tenant, false otherwise.
    /// Tenant scoping is enforced by the handler via <see cref="MindedExample.Infrastructure.Persistence.ICurrentUserAccessor"/>.
    /// </summary>
    public class ExistsUserInCurrentTenantQuery : IQuery<bool>, ILoggable
    {
        public int UserId { get; }

        public ExistsUserInCurrentTenantQuery(int userId, Guid? traceId = null)
        {
            UserId = userId;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "UserId: {UserId}";

        public string[] LoggingProperties => [nameof(UserId)];
    }
}
