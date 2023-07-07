using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;
using Minded.Framework.CQRS.Query.Trait;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Service.Category.Query
{
    public class GetCategoriesQuery : IQuery<IEnumerable<Data.Entity.Category>>, ICanCount, ICanTop, ICanSkip, ICanExpand, ICanOrderBy, ICanFilterExpression<Data.Entity.Category>, ILoggable
    {
        public bool CountOnly { get; set; }
        public bool Count { get; set; }
        public int CountValue { get; set; }
        public int? Top { get; set; }
        public int? Skip { get; set; }
        public string[] Expand { get; set; }
        public IList<OrderDescriptor> OrderBy { get; set; }
        public Expression<Func<Data.Entity.Category, bool>> Filter { get; set; }

        public GetCategoriesQuery(Guid? traceId = null)
        {
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "Count: {Count} - Top: {Top} - Skip: {Skip} - Expand: {Expand} - Order: {Order}";

        public object[] LoggingParameters => new object[] { Count, Top, Skip, Expand, OrderBy };
    }
}
