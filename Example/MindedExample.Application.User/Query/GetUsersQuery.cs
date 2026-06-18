using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Query;
using Minded.Framework.CQRS.Query.Trait;

namespace MindedExample.Application.User.Query
{
    /// <summary>
    /// Query to retrieve all users with optional OData filtering, sorting, and paging.
    /// </summary>
    [ValidateQuery]
    public class GetUsersQuery : IQuery<IQueryResponse<IEnumerable<MindedExample.Domain.User>>>, ICanCount, ICanTop, ICanSkip, ICanExpand, ICanOrderBy, ICanFilterExpression<MindedExample.Domain.User>, ILoggable
    {
        public bool CountOnly { get; set; }
        public bool Count { get; set; }
        public int CountValue { get; set; }
        public int? Top { get; set; }
        public int? Skip { get; set; }
        public string[] Expand { get; set; }
        public IList<OrderDescriptor> OrderBy { get; set; }
        public Expression<Func<MindedExample.Domain.User, bool>> Filter { get; set; }

        public GetUsersQuery(Guid? traceId = null)
        {
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "Top: {Top} - Skip: {Skip}";

        public string[] LoggingProperties => [nameof(Top), nameof(Skip)];
    }
}