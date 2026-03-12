using System.Threading;
using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using Service.User.Query;

namespace Service.User.QueryHandler
{
    /// <summary>
    /// Handler for retrieving a single user by ID.
    /// Returns null if the user is not found.
    /// </summary>
    public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, Data.Entity.User>
    {
        private readonly IMindedExampleContext _context;

        public GetUserByIdQueryHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a user by ID from the database.
        /// Uses AsNoTracking to prevent automatic loading of navigation properties (Categories, Transactions).
        /// Navigation properties can be loaded explicitly using $expand in OData queries.
        /// </summary>
        /// <param name="query">The query containing the user ID</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>User if found, null otherwise</returns>
        public async Task<Data.Entity.User> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == query.UserId, cancellationToken);
        }
    }
}

