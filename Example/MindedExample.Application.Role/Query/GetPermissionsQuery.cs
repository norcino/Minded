using System;
using System.Collections.Generic;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;
using MindedExample.Domain;

namespace MindedExample.Application.Role.Query
{
    /// <summary>
    /// Query to get all available permissions grouped by category.
    /// Returns a dictionary where keys are group names and values are permission name arrays.
    /// </summary>
    public class GetPermissionsQuery : IQuery<IQueryResponse<IReadOnlyDictionary<string, string[]>>>, ILoggable
    {
        public GetPermissionsQuery(Guid? traceId = null)
        {
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();
        public string LoggingTemplate => "GetPermissions";
        public string[] LoggingProperties => [];
    }
}
