using Microsoft.AspNetCore.OData.Query;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;
using System;
using System.Collections.Generic;

namespace Service.Transaction.Query
{
    /// <summary>
    /// Query to retrieve transactions with OData query options.
    /// Uses a computed property for logging the OData options string representation.
    /// </summary>
    public class GetTransactionsQuery : IQuery<List<Data.Entity.Transaction>>, ILoggable
    {
        public GetTransactionsQuery(ODataQueryOptions<Data.Entity.Transaction> options, Guid? traceId = null)
        {
            Options = options;
            TraceId = traceId ?? TraceId;
        }

        public ODataQueryOptions<Data.Entity.Transaction> Options { get; set; }

        /// <summary>
        /// Computed property for logging - returns string representation of OData options.
        /// </summary>
        public string ODataQueryOptionsString => Options?.ToString() ?? "None";

        public Guid TraceId { get; } = Guid.NewGuid();
        public string LoggingTemplate => "{ODataQueryOptions}";
        public string[] LoggingProperties => new[] { nameof(ODataQueryOptionsString) };
    }
}
