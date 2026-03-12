using System.Threading;
using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using Service.Transaction.Query;

namespace Service.Transaction.QueryHandler
{
    /// <summary>
    /// Handler for retrieving a single transaction by ID.
    /// Returns null if the transaction is not found.
    /// </summary>
    public class GetTransactionByIdQueryHandler : IQueryHandler<GetTransactionByIdQuery, Data.Entity.Transaction>
    {
        private readonly IMindedExampleContext _context;

        public GetTransactionByIdQueryHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a transaction from the database by ID.
        /// Uses AsNoTracking to prevent automatic loading of navigation properties (Category, User).
        /// Navigation properties can be loaded explicitly using $expand in OData queries.
        /// </summary>
        /// <param name="query">Query containing the transaction ID</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Transaction if found, null otherwise</returns>
        public Task<Data.Entity.Transaction> HandleAsync(GetTransactionByIdQuery query, CancellationToken cancellationToken = default)
        {
            return _context.Transactions
                .AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == query.TransactionId, cancellationToken);
        }
    }
}
