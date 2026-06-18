using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MindedExample.Application.User.Command;
using MindedExample.Domain;
using MindedExample.Infrastructure.Persistence;

namespace MindedExample.Application.User.Services
{
    /// <summary>
    /// Builds fully-populated <see cref="AuthResult"/> objects from domain User entities.
    /// Resolves the user's tenant and roles from the database.
    /// </summary>
    public class AuthResultBuilder : IAuthResultBuilder
    {
        private readonly IMindedExampleContext _context;

        /// <summary>
        /// Initializes a new <see cref="AuthResultBuilder"/>.
        /// </summary>
        public AuthResultBuilder(IMindedExampleContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<AuthResult> BuildAsync(
            Domain.User user,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            var tenant = user.TenantId.HasValue
                ? await _context.Tenants
                    .AsNoTracking()
                    .SingleOrDefaultAsync(t => t.Id == user.TenantId.Value, cancellationToken)
                : null;

            var roles = new List<string>();

            if (user.IsGlobalAdmin)
            {
                roles.Add(Roles.Admin);
            }

            if (_context is MindedExampleContext concreteContext && user.TenantId.HasValue)
            {
                roles = await concreteContext
                    .Set<Dictionary<string, object>>("UserRoles")
                    .Where(ur =>
                        (int)ur["TenantId"] == user.TenantId.Value &&
                        (int)ur["UserId"] == user.Id)
                    .Select(ur => (string)ur["RoleName"])
                    .ToListAsync(cancellationToken);
            }

            return new AuthResult
            {
                AccessToken = accessToken,
                User = new AuthUserResult
                {
                    Id = user.Id,
                    TenantId = user.TenantId,
                    Name = user.Name,
                    Surname = user.Surname,
                    Email = user.Email,
                    TenantRole = user.TenantRole,
                    IsGlobalAdmin = user.IsGlobalAdmin,
                    Roles = roles
                },
                Tenant = tenant == null
                    ? null
                    : new TenantResult { Id = tenant.Id, Name = tenant.Name }
            };
        }
    }
}
