using System.Threading;
using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using Service.Category.Query;

namespace Service.Category.QueryHandler
{
    /// <summary>
    /// Handler for retrieving a single category by ID.
    /// Returns null if the category is not found.
    /// </summary>
    public class GetCategoryByIdQueryHandler : IQueryHandler<GetCategoryByIdQuery, Data.Entity.Category>
    {
        private readonly IMindedExampleContext _context;

        public GetCategoryByIdQueryHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a category from the database by ID.
        /// Uses AsNoTracking to prevent automatic loading of navigation properties (Transactions).
        /// Navigation properties can be loaded explicitly using $expand in OData queries.
        /// </summary>
        /// <param name="query">Query containing the category ID</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Category if found, null otherwise</returns>
        public async Task<Data.Entity.Category> HandleAsync(GetCategoryByIdQuery query, CancellationToken cancellationToken = default)
        {
            return await _context.Categories
                .AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == query.CategoryId, cancellationToken);
        }
    }
}
