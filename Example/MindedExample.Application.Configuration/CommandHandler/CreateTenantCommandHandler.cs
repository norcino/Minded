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

            foreach (var rolePermissions in DefaultRolesDefinition.RolePermissions)
            {
                foreach (var permission in rolePermissions.Value)
                {
                    await concreteContext.Database.ExecuteSqlRawAsync(
                        "INSERT INTO RolePermissions (TenantId, RoleName, PermissionName) VALUES ({0}, {1}, {2})",
                        tenantId,
                        rolePermissions.Key,
                        permission);
                }
            }
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

            await concreteContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO UserRoles (TenantId, UserId, RoleName) VALUES ({0}, {1}, {2})",
                tenantId,
                userId,
                roleName);
        }
    }
}
