using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Query;
using Minded.Framework.CQRS.Query.Trait;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Service.User.Query
{
    /// <summary>
    /// Query to retrieve all users with optional filtering, sorting, and paging.
    /// Supports OData query options through ICanFilter, ICanOrderBy, ICanTop, ICanSkip traits.
    /// User's sensitive data (name, surname, email) will be protected in logs by the DataProtectionLoggingSanitizer.
    /// </summary>
    [ValidateQuery]
    public class GetUsersQuery : IQuery<IQueryResponse<IEnumerable<Data.Entity.User>>>, ICanCount, ICanTop, ICanSkip, ICanExpand, ICanOrderBy, ICanFilterExpression<Data.Entity.User>, ILoggable
    {
        public bool CountOnly { get; set; }
        public bool Count { get; set; }
        public int CountValue { get; set; }
        public int? Top { get; set; }
        public int? Skip { get; set; }
        public string[] Expand { get; set; }
        public IList<OrderDescriptor> OrderBy { get; set; }
        public Expression<Func<Data.Entity.User, bool>> Filter { get; set; }

        public GetUsersQuery(Guid? traceId = null)
        {
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "Getting users";

        public string[] LoggingProperties => Array.Empty<string>();
    }
}

