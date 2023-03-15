using System.Collections.Generic;
using Minded.Extensions.CQRS.OData;
using System.Threading.Tasks;
using Data.Context;
using Microsoft.Extensions.Logging;
using Minded.Framework.CQRS.Query;
using Service.Transaction.Query;
using System.Linq;

namespace Service.Transaction.QueryHandler
{
    public class GetTransactionsQueryHandler : IQueryHandler<GetTransactionsQuery, List<Data.Entity.Transaction>>
    {
        private readonly IMindedExampleContext _context;
        private readonly ILogger<IQueryHandler<GetTransactionsQuery, List<Data.Entity.Transaction>>> _logger;

        public GetTransactionsQueryHandler(IMindedExampleContext context, ILogger<IQueryHandler<GetTransactionsQuery, List<Data.Entity.Transaction>>> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Task<List<Data.Entity.Transaction>> HandleAsync(GetTransactionsQuery query)
        {
            return Task.FromResult(_context.Transactions.AsQueryable().ApplyODataQueryOptions(query.Options).ToList());
        }
    }
}
