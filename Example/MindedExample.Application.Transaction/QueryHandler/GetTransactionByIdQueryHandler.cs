using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.Transaction.Query;

namespace MindedExample.Application.Transaction.QueryHandler
{
    /// <summary>
    /// Handler for retrieving a single transaction by ID.
    /// Returns null if the transaction is not found.
    /// </summary>
    public class GetTransactionByIdQueryHandler : IQueryHandler<GetTransactionByIdQuery, MindedExample.Domain.Transaction>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetTransactionByIdQueryHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Retrieves a transaction from the database by ID.
        /// Uses AsNoTracking to prevent automatic loading of navigation properties (Category, User).
        /// Navigation properties can be loaded explicitly using $expand in OData queries.
        /// </summary>
        /// <param name="query">Query containing the transaction ID</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Transaction if found, null otherwise</returns>
        public Task<MindedExample.Domain.Transaction> HandleAsync(GetTransactionByIdQuery query, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return Task.FromResult<MindedExample.Domain.Transaction>(null);
            }

            return _context.Transactions
                .AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == query.TransactionId && c.User.TenantId == _currentUserAccessor.TenantId.Value, cancellationToken);
        }
    }
}
