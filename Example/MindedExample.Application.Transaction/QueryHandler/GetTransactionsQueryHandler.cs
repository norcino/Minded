using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Minded.Extensions.CQRS.OData;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.Transaction.Query;

namespace MindedExample.Application.Transaction.QueryHandler
{
    /// <summary>
    /// Handler for retrieving transactions with OData query support.
    /// Supports filtering, ordering, paging, and counting through OData query options.
    /// </summary>
    public class GetTransactionsQueryHandler : IQueryHandler<GetTransactionsQuery, List<MindedExample.Domain.Transaction>>
    {
        private readonly IMindedExampleContext _context;
        private readonly ILogger<IQueryHandler<GetTransactionsQuery, List<MindedExample.Domain.Transaction>>> _logger;

        public GetTransactionsQueryHandler(IMindedExampleContext context, ILogger<IQueryHandler<GetTransactionsQuery, List<MindedExample.Domain.Transaction>>> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves transactions from the database with OData query options applied.
        /// ApplyODataQueryOptions returns IEnumerable which is already materialized, so we convert to List.
        /// </summary>
        /// <param name="query">Query containing OData options</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>List of transactions matching the query criteria</returns>
        public Task<List<MindedExample.Domain.Transaction>> HandleAsync(GetTransactionsQuery query, CancellationToken cancellationToken = default)
        {
            IEnumerable<MindedExample.Domain.Transaction> result = _context.Transactions.AsQueryable().ApplyODataQueryOptions(query.Options);
            return Task.FromResult(result.ToList());
        }
    }
}
