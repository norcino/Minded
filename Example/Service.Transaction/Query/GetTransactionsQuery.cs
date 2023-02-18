using Microsoft.AspNet.OData.Query;
using Minded.Framework.CQRS.Query;
using System.Collections.Generic;

namespace Service.Transaction.Query
{
    public class GetTransactionsQuery : IQuery<List<Data.Entity.Transaction>>
    {
        public GetTransactionsQuery(ODataQueryOptions<Data.Entity.Transaction> options)
        {
            Options = options;
        }

        public ODataQueryOptions<Data.Entity.Transaction> Options { get; set; }
    }
}
