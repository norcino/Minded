using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.User.Query;
using MindedExample.Infrastructure.Persistence;

namespace MindedExample.Application.User.QueryHandler
{
    /// <summary>
    /// Handler for <see cref="ExistsUserInCurrentTenantQuery"/>.
    /// Returns true if the specified user exists within the current caller's tenant, otherwise false.
    /// </summary>
    public class ExistsUserInCurrentTenantQueryHandler : IQueryHandler<ExistsUserInCurrentTenantQuery, bool>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public ExistsUserInCurrentTenantQueryHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<bool> HandleAsync(ExistsUserInCurrentTenantQuery query, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return false;
            }

            return await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == query.UserId && u.TenantId == _currentUserAccessor.TenantId.Value, cancellationToken);
        }
    }
}
