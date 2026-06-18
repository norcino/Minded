using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.User.Query;

namespace MindedExample.Application.User.QueryHandler
{
    /// <summary>
    /// Handler for retrieving a single user by ID.
    /// Returns null if the user is not found.
    /// </summary>
    public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, MindedExample.Domain.User>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetUserByIdQueryHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Retrieves a user by ID from the database.
        /// Uses AsNoTracking to prevent automatic loading of navigation properties (Categories, Transactions).
        /// Navigation properties can be loaded explicitly using $expand in OData queries.
        /// </summary>
        /// <param name="query">The query containing the user ID</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>User if found, null otherwise</returns>
        public async Task<MindedExample.Domain.User> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return null;
            }

            return await _context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == query.UserId && u.TenantId == _currentUserAccessor.TenantId.Value, cancellationToken);
        }
    }
}

