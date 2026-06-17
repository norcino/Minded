using Minded.Extensions.Authorization.Attributes;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Query;
using Minded.Framework.CQRS.Query.Trait;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MindedExample.Application.Transaction.Query
{
    /// <summary>
    /// Query to retrieve a collection of transactions, supporting OData-style filtering,
    /// ordering, paging, and expansion through the Minded trait interface system.
    /// </summary>
    [ValidateQuery]
    [RequireClaim("is_global_admin", "false")]
    public class GetTransactionsQuery :
        IQuery<IQueryResponse<IEnumerable<MindedExample.Domain.Transaction>>>,
        ICanCount, ICanTop, ICanSkip, ICanExpand, ICanOrderBy,
        ICanFilterExpression<MindedExample.Domain.Transaction>,
        ILoggable
    {
        public GetTransactionsQuery(Guid? traceId = null)
        {
            TraceId = traceId ?? TraceId;
        }

        public bool CountOnly { get; set; }
        public bool Count { get; set; }
        public int CountValue { get; set; }
        public int? Top { get; set; }
        public int? Skip { get; set; }
        public string[] Expand { get; set; }
        public IList<OrderDescriptor> OrderBy { get; set; }
        public Expression<Func<MindedExample.Domain.Transaction, bool>> Filter { get; set; }

        public Guid TraceId { get; } = Guid.NewGuid();
        public string LoggingTemplate => "Count: {Count} - Top: {Top} - Skip: {Skip}";
        public string[] LoggingProperties => [nameof(Count), nameof(Top), nameof(Skip)];
    }
}