using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.User.Query;

namespace MindedExample.Application.User.QueryHandler
{
    /// <summary>
    /// Handler for retrieving all users with optional filtering, sorting, and paging.
    /// Supports OData query options through the query traits.
    /// </summary>
    public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, IQueryResponse<IEnumerable<MindedExample.Domain.User>>>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetUsersQueryHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Retrieves users from the database with optional filtering, sorting, and paging.
        /// Applies OData query options if specified in the query.
        /// </summary>
        /// <param name="query">The query containing filter, sort, and paging options</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Query response with the list of users</returns>
        public async Task<IQueryResponse<IEnumerable<MindedExample.Domain.User>>> HandleAsync(GetUsersQuery query, CancellationToken cancellationToken = default)
        {
            var tenantId = _currentUserAccessor.TenantId;
            var usersQuery = _context.Users.AsQueryable();
            if (tenantId.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.TenantId == tenantId.Value);
            }

            var result = await query.ApplyQueryTo(usersQuery).ToListAsync(cancellationToken);
            var response = new QueryResponse<IEnumerable<MindedExample.Domain.User>>(result);

            return response;
        }
    }
}

