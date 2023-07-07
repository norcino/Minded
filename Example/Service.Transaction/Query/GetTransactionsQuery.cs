using Microsoft.AspNet.OData.Query;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;
using System;
using System.Collections.Generic;

namespace Service.Transaction.Query
{
    public class GetTransactionsQuery : IQuery<List<Data.Entity.Transaction>>, ILoggable
    {
        public GetTransactionsQuery(ODataQueryOptions<Data.Entity.Transaction> options, Guid? traceId = null)
        {
            Options = options;
            TraceId = traceId ?? TraceId;
        }

        public ODataQueryOptions<Data.Entity.Transaction> Options { get; set; }

        public Guid TraceId { get; } = Guid.NewGuid();
        public string LoggingTemplate => "{ODataQueryOptions}";
        public object[] LoggingParameters => new object[] { Options.ToString() };
    }
}
