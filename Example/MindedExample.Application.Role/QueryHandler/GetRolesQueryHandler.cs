using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.Role.Query;
using MindedExample.Domain;

namespace MindedExample.Application.Role.QueryHandler
{
    public class GetRolesQueryHandler : IQueryHandler<GetRolesQuery, IQueryResponse<IEnumerable<RoleDto>>>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetRolesQueryHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<IQueryResponse<IEnumerable<RoleDto>>> HandleAsync(GetRolesQuery query, CancellationToken cancellationToken = default)
        {
            var result = new List<RoleDto>();
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return new QueryResponse<IEnumerable<RoleDto>>(result);
            }

            var tenantId = _currentUserAccessor.TenantId.Value;

            if (_context is MindedExampleContext concreteContext)
            {
                // Query the RolePermissions shared-type entity
                var rolePermissions = await concreteContext.Set<Dictionary<string, object>>("RolePermissions")
                    .Where(rp => (int)rp["TenantId"] == tenantId)
                    .ToListAsync(cancellationToken);

                var grouped = rolePermissions
                    .GroupBy(rp => (string)rp["RoleName"])
                    .Select(g => new RoleDto
                    {
                        Name = g.Key,
                        Permissions = g.Select(rp => (string)rp["PermissionName"]).ToArray()
                    })
                    .ToList();

                result.AddRange(grouped);
            }

            return new QueryResponse<IEnumerable<RoleDto>>(result);
        }
    }
}
