using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using Service.User.Query;

namespace Service.User.QueryHandler
{
    /// <summary>
    /// Handler for retrieving all users with optional filtering, sorting, and paging.
    /// Supports OData query options through the query traits.
    /// </summary>
    public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, IQueryResponse<IEnumerable<Data.Entity.User>>>
    {
        private readonly IMindedExampleContext _context;

        public GetUsersQueryHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves users from the database with optional filtering, sorting, and paging.
        /// Applies OData query options if specified in the query.
        /// </summary>
        /// <param name="query">The query containing filter, sort, and paging options</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Query response with the list of users</returns>
        public async Task<IQueryResponse<IEnumerable<Data.Entity.User>>> HandleAsync(GetUsersQuery query, CancellationToken cancellationToken = default)
        {
            var result = await query.ApplyQueryTo(_context.Users.AsQueryable()).ToListAsync(cancellationToken);
            var response = new QueryResponse<IEnumerable<Data.Entity.User>>(result);

            return response;
        }
    }
}

