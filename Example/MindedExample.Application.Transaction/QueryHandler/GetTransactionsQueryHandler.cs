using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.Transaction.Query;

namespace MindedExample.Application.Transaction.QueryHandler
{
    /// <summary>
    /// Handler for retrieving a collection of transactions.
    /// Applies tenant isolation and then delegates OData trait options (filter, order, paging, expand)
    /// to the query's ApplyQueryTo helper, keeping the handler free of HTTP/OData concerns.
    /// </summary>
    public class GetTransactionsQueryHandler : IQueryHandler<GetTransactionsQuery, IQueryResponse<IEnumerable<MindedExample.Domain.Transaction>>>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetTransactionsQueryHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Retrieves transactions scoped to the current tenant with all OData query traits applied.
        /// </summary>
        /// <param name="query">Query carrying the trait configuration (filter, order, paging, expand).</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
        /// <returns>Query response wrapping the matching transactions.</returns>
        public async Task<IQueryResponse<IEnumerable<MindedExample.Domain.Transaction>>> HandleAsync(
            GetTransactionsQuery query, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return new QueryResponse<IEnumerable<MindedExample.Domain.Transaction>>(new List<MindedExample.Domain.Transaction>());
            }

            var tenantId = _currentUserAccessor.TenantId.Value;
            var transactionsQuery = _context.Transactions
                .Where(t => t.User.TenantId == tenantId)
                .AsQueryable();

            // Cap Top to the maximum page size; ApplyQueryTo defaults to 100 when Top is null.
            const int maxPageSize = 100;
            if (query.Top.HasValue && query.Top.Value > maxPageSize)
                query.Top = maxPageSize;

            List<MindedExample.Domain.Transaction> result = await query
                .ApplyQueryTo(transactionsQuery)
                .ToListAsync(cancellationToken);

            return new QueryResponse<IEnumerable<MindedExample.Domain.Transaction>>(result);
        }
    }
}
