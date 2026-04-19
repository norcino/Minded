using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.Role.Query;

namespace MindedExample.Application.Role.QueryHandler
{
    public class GetUsersWithRolesQueryHandler : IQueryHandler<GetUsersWithRolesQuery, IQueryResponse<IEnumerable<Domain.User>>>
    {
        private readonly IMindedExampleContext _context;

        public GetUsersWithRolesQueryHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        public async Task<IQueryResponse<IEnumerable<Domain.User>>> HandleAsync(GetUsersWithRolesQuery query, CancellationToken cancellationToken = default)
        {
            List<Domain.User> users = await _context.Users.ToListAsync(cancellationToken);

            if (_context is MindedExampleContext concreteContext)
            {
                // Load all user-role assignments
                var userRoles = await concreteContext.Set<Dictionary<string, object>>("UserRoles")
                    .ToListAsync(cancellationToken);

                var userRolesLookup = userRoles
                    .GroupBy(ur => (int)ur["UserId"])
                    .ToDictionary(g => g.Key, g => g.Select(ur => (string)ur["RoleName"]).ToList());

                foreach (var user in users)
                {
                    if (userRolesLookup.TryGetValue(user.Id, out var roles))
                    {
                        user.Roles = new HashSet<string>(roles);
                    }
                }
            }

            return new QueryResponse<IEnumerable<Domain.User>>(users);
        }
    }
}
