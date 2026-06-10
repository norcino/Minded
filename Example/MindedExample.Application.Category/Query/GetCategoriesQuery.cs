using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Extensions.Authorization.Attributes;
using Minded.Framework.CQRS.Query;
using Minded.Framework.CQRS.Query.Trait;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MindedExample.Application.Category.Query
{
    [ValidateQuery]
    [RequireClaim("is_global_admin", "false")]
    public class GetCategoriesQuery : IQuery<IQueryResponse<IEnumerable<Domain.Category>>>, ICanCount, ICanTop, ICanSkip, ICanExpand, ICanOrderBy, ICanFilterExpression<Domain.Category>, ILoggable
    {
        public bool CountOnly { get; set; }
        public bool Count { get; set; }
        public int CountValue { get; set; }
        public int? Top { get; set; }
        public int? Skip { get; set; }
        public string[] Expand { get; set; }
        public IList<OrderDescriptor> OrderBy { get; set; }
        public Expression<Func<Domain.Category, bool>> Filter { get; set; }

        public GetCategoriesQuery(Guid? traceId = null)
        {
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "Count: {Count} - Top: {Top} - Skip: {Skip} - Expand: {Expand} - Order: {Order}";

        public string[] LoggingProperties => [nameof(Count), nameof(Top), nameof(Skip), nameof(Expand), nameof(OrderBy)];
    }
}