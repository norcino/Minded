using System.Threading;
using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using Service.Category.Query;

namespace Service.Category.QueryHandler
{
    /// <summary>
    /// Handler for <see cref="ExistsCategoryByIdQuery"/>.
    /// Returns true if the Category exists, otherwise false.
    /// Uses AsNoTracking for performance.
    /// </summary>
    public class ExistsCategoryByIdQueryHandler : IQueryHandler<ExistsCategoryByIdQuery, bool>
    {
        private readonly IMindedExampleContext _context;

        public ExistsCategoryByIdQueryHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        public async Task<bool> HandleAsync(ExistsCategoryByIdQuery query, CancellationToken cancellationToken = default)
        {
            return await _context.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Id == query.CategoryId, cancellationToken);
        }
    }
}
