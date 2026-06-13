using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.Configuration.Command;
using MindedExample.Domain;
using MindedExample.Infrastructure.Persistence;

namespace MindedExample.Application.Configuration.CommandHandler
{
    /// <summary>
    /// Handles creation of tenants and their legal owner users.
    /// </summary>
    public class CreateTenantCommandHandler : ICommandHandler<CreateTenantCommand, CreateTenantResult>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IPasswordHasher<User> _passwordHasher;

        public CreateTenantCommandHandler(
            IMindedExampleContext context,
            ICurrentUserAccessor currentUserAccessor,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _passwordHasher = passwordHasher;
        }

        public async Task<ICommandResponse<CreateTenantResult>> HandleAsync(CreateTenantCommand command, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.IsGlobalAdmin)
            {
                throw new SecurityException();
            }

            var tenantName = command.Name.Trim();
            var ownerEmail = command.LegalOwnerEmail.Trim().ToLowerInvariant();

            var tenant = new Tenant { Name = tenantName };
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync(cancellationToken);

            var owner = new User
            {
                Name = command.LegalOwnerName?.Trim(),
                Surname = command.LegalOwnerSurname?.Trim(),
                Email = ownerEmail,
                TenantId = tenant.Id,
                TenantRole = TenantMemberRoles.Owner,
                IsActive = true,
                IsGlobalAdmin = false
            };
            owner.PasswordHash = _passwordHasher.HashPassword(owner, command.LegalOwnerPassword);

            _context.Users.Add(owner);
            await _context.SaveChangesAsync(cancellationToken);

            tenant.LegalOwnerUserId = owner.Id;
            await _context.SaveChangesAsync(cancellationToken);

            await EnsureTenantRolePermissionsInitializedAsync(tenant.Id, cancellationToken);
            await AssignUserRoleAsync(tenant.Id, owner.Id, Roles.TenantAdmin);

            var result = new CreateTenantResult
            {
                Id = tenant.Id,
                Name = tenant.Name
            };

            return new CommandResponse<CreateTenantResult>(result)
            {
                Successful = true
            };
        }

        private async Task EnsureTenantRolePermissionsInitializedAsync(int tenantId, CancellationToken cancellationToken)
        {
            if (_context is not MindedExampleContext concreteContext)
            {
                return;
            }

            var hasAny = await concreteContext.Set<Dictionary<string, object>>("RolePermissions")
                .AnyAsync(rp => (int)rp["TenantId"] == tenantId, cancellationToken);

            if (hasAny)
            {
                return;
            }

            // Inserted through the shared-type entity set (not raw SQL) so EF generates
            // correctly quoted, schema-qualified SQL for every database provider.
            var rolePermissionsSet = concreteContext.Set<Dictionary<string, object>>("RolePermissions");
            foreach (var rolePermissions in DefaultRolesDefinition.RolePermissions)
            {
                foreach (var permission in rolePermissions.Value)
                {
                    rolePermissionsSet.Add(new Dictionary<string, object>
                    {
                        ["TenantId"] = tenantId,
                        ["RoleName"] = rolePermissions.Key,
                        ["PermissionName"] = permission
                    });
                }
            }

            await concreteContext.SaveChangesAsync(cancellationToken);
        }

        private async Task AssignUserRoleAsync(int tenantId, int userId, string roleName)
        {
            if (_context is not MindedExampleContext concreteContext)
            {
                return;
            }

            var roleExists = await concreteContext.Set<Dictionary<string, object>>("UserRoles")
                .AnyAsync(ur =>
                    (int)ur["TenantId"] == tenantId &&
                    (int)ur["UserId"] == userId &&
                    (string)ur["RoleName"] == roleName);

            if (roleExists)
            {
                return;
            }

            concreteContext.Set<Dictionary<string, object>>("UserRoles").Add(new Dictionary<string, object>
            {
                ["TenantId"] = tenantId,
                ["UserId"] = userId,
                ["RoleName"] = roleName
            });
            await concreteContext.SaveChangesAsync();
        }
    }
}
