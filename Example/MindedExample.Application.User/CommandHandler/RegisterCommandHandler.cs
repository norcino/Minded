using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Minded.Extensions.Exception;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.Common;
using MindedExample.Application.User.Command;
using MindedExample.Application.User.Services;
using MindedExample.Domain;
using MindedExample.Infrastructure.Persistence;

namespace MindedExample.Application.User.CommandHandler
{
    /// <summary>
    /// Handles <see cref="RegisterCommand"/> across three registration flows:
    /// <list type="bullet">
    ///   <item><b>create-tenant</b> (default): registers the user as owner of a new tenant.</item>
    ///   <item><b>join-tenant</b>: creates a pending join request for an existing tenant.</item>
    ///   <item><b>from-invite</b>: registers the user into a tenant via an invite token.</item>
    /// </list>
    /// Duplicate email and pending join-request conflicts return HTTP 409 Conflict via
    /// the custom <c>MindedExampleRestRulesProvider</c>.
    /// </summary>
    public class RegisterCommandHandler : ICommandHandler<RegisterCommand, AuthResult>
    {
        private const string ModeJoinTenant = "join-tenant";

        private readonly IMindedExampleContext _context;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IPasswordService _passwordService;
        private readonly IAuthResultBuilder _authResultBuilder;

        /// <summary>Initializes a new <see cref="RegisterCommandHandler"/>.</summary>
        public RegisterCommandHandler(
            IMindedExampleContext context,
            IJwtTokenService jwtTokenService,
            IPasswordService passwordService,
            IAuthResultBuilder authResultBuilder)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _passwordService = passwordService;
            _authResultBuilder = authResultBuilder;
        }

        /// <inheritdoc />
        public async Task<ICommandResponse<AuthResult>> HandleAsync(RegisterCommand command, CancellationToken cancellationToken = default)
        {
            var email = command.Email.Trim().ToLowerInvariant();

            var existing = await _context.Users.AsNoTracking()
                .SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (existing != null)
                return CommandResponse<AuthResult>.Error(
                    new OutcomeEntry(nameof(command.Email), "{0} is already in use", attemptedValue: null, Severity.Error, AuthErrorCodes.EmailAlreadyExists));

            var hasPendingJoinRequest = await _context.TenantJoinRequests.AsNoTracking()
                .AnyAsync(r => r.Email == email && r.ProcessedAtUtc == null, cancellationToken);
            if (hasPendingJoinRequest)
                return CommandResponse<AuthResult>.Error(
                    new OutcomeEntry(nameof(command.Email), "{0} has a pending join request", attemptedValue: null, Severity.Error, AuthErrorCodes.PendingJoinRequestExists));

            if (!string.IsNullOrWhiteSpace(command.InviteToken))
                return await RegisterFromInviteAsync(command, email, cancellationToken);

            var mode = string.IsNullOrWhiteSpace(command.Mode)
                ? string.Empty
                : command.Mode.Trim().ToLowerInvariant();

            return mode == ModeJoinTenant
                ? await RegisterJoinTenantAsync(command, email, cancellationToken)
                : await RegisterCreateTenantAsync(command, email, cancellationToken);
        }

        private async Task<ICommandResponse<AuthResult>> RegisterCreateTenantAsync(
            RegisterCommand command, string email, CancellationToken cancellationToken)
        {
            var tenant = new Tenant
            {
                Name = string.IsNullOrWhiteSpace(command.TenantName)
                    ? $"{command.Name} {command.Surname}".Trim() + " Tenant"
                    : command.TenantName.Trim()
            };
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync(cancellationToken);

            var user = new Domain.User
            {
                Name = command.Name?.Trim(),
                Surname = command.Surname?.Trim(),
                Email = email,
                TenantId = tenant.Id,
                TenantRole = TenantMemberRoles.Owner,
                IsActive = true,
                IsGlobalAdmin = false
            };
            user.PasswordHash = _passwordService.HashPassword(user, command.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            tenant.LegalOwnerUserId = user.Id;
            await _context.SaveChangesAsync(cancellationToken);

            await EnsureTenantRolePermissionsInitializedAsync(tenant.Id, cancellationToken);
            await AssignUserRoleAsync(tenant.Id, user.Id, Roles.TenantAdmin, cancellationToken);

            var token = _jwtTokenService.CreateAccessToken(user);
            return CommandResponse<AuthResult>.Success(await _authResultBuilder.BuildAsync(user, token, cancellationToken));
        }

        private async Task<ICommandResponse<AuthResult>> RegisterJoinTenantAsync(
            RegisterCommand command, string email, CancellationToken cancellationToken)
        {
            var tenantName = command.TenantName.Trim();
            var tenant = await _context.Tenants
                .SingleOrDefaultAsync(t => t.Name == tenantName, cancellationToken);

            if (tenant == null)
                return CommandResponse<AuthResult>.Error(
                    new OutcomeEntry(nameof(command.TenantName), "{0} was not found", attemptedValue: null, Severity.Error, GenericErrorCodes.SubjectNotFound));

            var tempUser = new Domain.User { Email = email };
            _context.TenantJoinRequests.Add(new TenantJoinRequest
            {
                TenantId = tenant.Id,
                Name = command.Name?.Trim(),
                Surname = command.Surname?.Trim(),
                Email = email,
                PasswordHash = _passwordService.HashPassword(tempUser, command.Password),
                CreatedAtUtc = DateTime.UtcNow,
                Approved = false
            });
            await _context.SaveChangesAsync(cancellationToken);

            return CommandResponse<AuthResult>.Success(new AuthResult
            {
                PendingApproval = true,
                Message = "Registration completed. A tenant administrator must approve your request before you can log in."
            });
        }

        private async Task<ICommandResponse<AuthResult>> RegisterFromInviteAsync(
            RegisterCommand command, string email, CancellationToken cancellationToken)
        {
            var tokenOrCode = command.InviteToken.Trim();
            var invite = await _context.TenantInvites
                .SingleOrDefaultAsync(i => i.Token == tokenOrCode || i.Code == tokenOrCode, cancellationToken);

            if (invite == null || invite.UsedAtUtc != null || invite.ExpiresAtUtc < DateTime.UtcNow)
                return CommandResponse<AuthResult>.Error(
                    new OutcomeEntry(nameof(command.InviteToken), "{0} is invalid or expired", attemptedValue: null, Severity.Error, GenericErrorCodes.ValidationFailed));

            if (!string.IsNullOrWhiteSpace(invite.Email) &&
                !string.Equals(invite.Email, email, StringComparison.OrdinalIgnoreCase))
                return CommandResponse<AuthResult>.Error(
                    new OutcomeEntry(nameof(command.Email), "{0} does not match the invite email", attemptedValue: null, Severity.Error, GenericErrorCodes.ValidationFailed));

            var user = new Domain.User
            {
                Name = command.Name?.Trim(),
                Surname = command.Surname?.Trim(),
                Email = email,
                TenantId = invite.TenantId,
                TenantRole = TenantMemberRoles.Member,
                IsActive = true,
                IsGlobalAdmin = false
            };
            user.PasswordHash = _passwordService.HashPassword(user, command.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            invite.UsedAtUtc = DateTime.UtcNow;
            invite.UsedByUserId = user.Id;

            await EnsureTenantRolePermissionsInitializedAsync(invite.TenantId, cancellationToken);
            await AssignUserRoleAsync(invite.TenantId, user.Id, Roles.User, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var token = _jwtTokenService.CreateAccessToken(user);
            return CommandResponse<AuthResult>.Success(await _authResultBuilder.BuildAsync(user, token, cancellationToken));
        }

        private async Task EnsureTenantRolePermissionsInitializedAsync(int tenantId, CancellationToken cancellationToken)
        {
            if (_context is not MindedExampleContext concreteContext) return;

            var hasAny = await concreteContext.Set<Dictionary<string, object>>("RolePermissions")
                .AnyAsync(rp => (int)rp["TenantId"] == tenantId, cancellationToken);
            if (hasAny) return;

            var rolePermissions = concreteContext.Set<Dictionary<string, object>>("RolePermissions");
            foreach (var kvp in DefaultRolesDefinition.RolePermissions)
                foreach (var permission in kvp.Value)
                    rolePermissions.Add(new Dictionary<string, object>
                    {
                        ["TenantId"] = tenantId,
                        ["RoleName"] = kvp.Key,
                        ["PermissionName"] = permission
                    });

            await concreteContext.SaveChangesAsync(cancellationToken);
        }

        private async Task AssignUserRoleAsync(int tenantId, int userId, string roleName, CancellationToken cancellationToken)
        {
            if (_context is not MindedExampleContext concreteContext) return;

            concreteContext.Set<Dictionary<string, object>>("UserRoles").Add(new Dictionary<string, object>
            {
                ["TenantId"] = tenantId, ["UserId"] = userId, ["RoleName"] = roleName
            });
            await concreteContext.SaveChangesAsync(cancellationToken);
        }
    }
}
