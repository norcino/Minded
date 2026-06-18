using System;
using System.Collections.Generic;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;

namespace MindedExample.Application.User.Query
{
    /// <summary>
    /// Query to retrieve all users of the current tenant for the tenant administration view.
    /// Returns a lightweight DTO that excludes sensitive fields such as password hash.
    /// </summary>
    public class GetTenantAdminUsersQuery : IQuery<IQueryResponse<IEnumerable<TenantAdminUserDto>>>, ILoggable
    {
        /// <summary>
        /// Initializes a new <see cref="GetTenantAdminUsersQuery"/>.
        /// </summary>
        public GetTenantAdminUsersQuery(Guid? traceId = null)
        {
            TraceId = traceId ?? TraceId;
        }

        /// <inheritdoc />
        public Guid TraceId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public string LoggingTemplate => "Getting tenant admin users list";

        /// <inheritdoc />
        public string[] LoggingProperties => [];
    }
}
