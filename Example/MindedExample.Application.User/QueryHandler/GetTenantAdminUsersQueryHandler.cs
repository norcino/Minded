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
    /// Handler for retrieving the list of tenant users for the tenant administration view.
    /// Returns a lightweight DTO ordered by name and surname.
    /// </summary>
    public class GetTenantAdminUsersQueryHandler : IQueryHandler<GetTenantAdminUsersQuery, IQueryResponse<IEnumerable<TenantAdminUserDto>>>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        /// <summary>
        /// Initializes a new <see cref="GetTenantAdminUsersQueryHandler"/>.
        /// </summary>
        public GetTenantAdminUsersQueryHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <inheritdoc />
        public async Task<IQueryResponse<IEnumerable<TenantAdminUserDto>>> HandleAsync(
            GetTenantAdminUsersQuery query, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return new QueryResponse<IEnumerable<TenantAdminUserDto>>(new List<TenantAdminUserDto>());
            }

            var users = await _context.Users
                .AsNoTracking()
                .Where(u => u.TenantId == _currentUserAccessor.TenantId.Value)
                .OrderBy(u => u.Name)
                .ThenBy(u => u.Surname)
                .Select(u => new TenantAdminUserDto
                {
                    Id = u.Id,
                    TenantId = u.TenantId,
                    Name = u.Name,
                    Surname = u.Surname,
                    Email = u.Email,
                    TenantRole = u.TenantRole
                })
                .ToListAsync(cancellationToken);

            return new QueryResponse<IEnumerable<TenantAdminUserDto>>(users);
        }
    }
}
