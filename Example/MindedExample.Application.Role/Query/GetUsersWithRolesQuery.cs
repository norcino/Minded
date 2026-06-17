using System;
using System.Collections.Generic;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;

namespace MindedExample.Application.Role.Query
{
    /// <summary>
    /// Query to get all users with their assigned roles.
    /// This is in the Role application module since it's specifically for the admin role management view.
    /// </summary>
    public class GetUsersWithRolesQuery : IQuery<IQueryResponse<IEnumerable<Domain.User>>>, ILoggable
    {
        public GetUsersWithRolesQuery(Guid? traceId = null)
        {
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();
        public string LoggingTemplate => "GetUsersWithRoles";
        public string[] LoggingProperties => [];
    }
}
