using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.Category.Query;

namespace MindedExample.Application.Category.QueryHandler
{
    /// <summary>
    /// Handler for <see cref="ExistsCategoryInCurrentTenantQuery"/>.
    /// Returns true if the Category exists in the current caller's tenant, otherwise false.
    /// Uses AsNoTracking for performance.
    /// </summary>
    public class ExistsCategoryInCurrentTenantQueryHandler : IQueryHandler<ExistsCategoryInCurrentTenantQuery, bool>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public ExistsCategoryInCurrentTenantQueryHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<bool> HandleAsync(ExistsCategoryInCurrentTenantQuery query, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return false;
            }

            return await _context.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Id == query.CategoryId && c.User.TenantId == _currentUserAccessor.TenantId.Value, cancellationToken);
        }
    }
}
