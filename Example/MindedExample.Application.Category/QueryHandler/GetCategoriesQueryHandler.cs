using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.Category.Query;

namespace MindedExample.Application.Category.QueryHandler
{
    /// <summary>
    /// Handler for retrieving a collection of categories for the current tenant.
    /// Logging is handled by the decorator pipeline via ILoggable on <see cref="GetCategoriesQuery"/>.
    /// </summary>
    public class GetCategoriesQueryHandler : IQueryHandler<GetCategoriesQuery, IQueryResponse<IEnumerable<MindedExample.Domain.Category>>>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetCategoriesQueryHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<IQueryResponse<IEnumerable<MindedExample.Domain.Category>>> HandleAsync(GetCategoriesQuery query, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return new QueryResponse<IEnumerable<MindedExample.Domain.Category>>(new List<MindedExample.Domain.Category>());
            }

            var tenantId = _currentUserAccessor.TenantId.Value;
            var categoriesQuery = _context.Categories.Where(c => c.User.TenantId == tenantId).AsQueryable();
            List<MindedExample.Domain.Category> result = await query.ApplyQueryTo(categoriesQuery).ToListAsync(cancellationToken);
            var response = new QueryResponse<IEnumerable<MindedExample.Domain.Category>>(result);

            return response;
        }
    } 
}
