using System;
using System.Collections.Generic;
using Minded.Extensions.Authorization.Attributes;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;

namespace MindedExample.Application.Configuration.Query
{
    /// <summary>
    /// Query to retrieve tenant-level summaries for global administration.
    /// Requires the caller to be a global administrator.
    /// </summary>
    [RequireClaim("is_global_admin", "true")]
    public class GetAdminTenantSummariesQuery : IQuery<IQueryResponse<IEnumerable<TenantSummaryModel>>>, ILoggable
    {
        public GetAdminTenantSummariesQuery(Guid? traceId = null)
        {
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "Retrieving tenant administration summaries";

        public string[] LoggingProperties => Array.Empty<string>();
    }

    /// <summary>
    /// Tenant summary payload used by global administration endpoints.
    /// </summary>
    public class TenantSummaryModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int? LegalOwnerUserId { get; set; }

        public string LegalOwnerEmail { get; set; }

        public int ActiveUsersCount { get; set; }

        public int CategoriesCount { get; set; }

        public int TransactionsCount { get; set; }
    }
}
