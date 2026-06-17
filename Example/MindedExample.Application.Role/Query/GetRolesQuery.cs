using System;
using System.Collections.Generic;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;
using MindedExample.Domain;

namespace MindedExample.Application.Role.Query
{
    public class GetRolesQuery : IQuery<IQueryResponse<IEnumerable<RoleDto>>>, ILoggable
    {
        public GetRolesQuery(Guid? traceId = null)
        {
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();
        public string LoggingTemplate => "GetRoles";
        public string[] LoggingProperties => [];
    }
}
