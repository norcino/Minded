using System;
using System.Collections.Generic;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;

namespace MindedExample.Application.User.Query
{
    /// <summary>
    /// Query to retrieve all pending join requests for the current tenant.
    /// Returns a lightweight DTO that excludes sensitive fields such as the password hash.
    /// </summary>
    public class GetTenantJoinRequestsQuery : IQuery<IQueryResponse<IEnumerable<TenantJoinRequestSummaryDto>>>, ILoggable
    {
        /// <summary>
        /// Initializes a new <see cref="GetTenantJoinRequestsQuery"/>.
        /// </summary>
        public GetTenantJoinRequestsQuery(Guid? traceId = null)
        {
            TraceId = traceId ?? TraceId;
        }

        /// <inheritdoc />
        public Guid TraceId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public string LoggingTemplate => "Getting pending tenant join requests";

        /// <inheritdoc />
        public string[] LoggingProperties => [];
    }
}
