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
    /// Handles <see cref="AcceptInviteCommand"/> by validating the invite, creating the
    /// new user account inside the inviting tenant, assigning the default User role, and
    /// returning an <see cref="AuthResult"/> so the new member can immediately log in.
    /// </summary>
    public class AcceptInviteCommandHandler : ICommandHandler<AcceptInviteCommand, AuthResult>
    {
        private readonly IMindedExampleContext _context;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IPasswordService _passwordService;
        private readonly IAuthResultBuilder _authResultBuilder;

        /// <summary>Initializes a new <see cref="AcceptInviteCommandHandler"/>.</summary>
        public AcceptInviteCommandHandler(
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
        public async Task<ICommandResponse<AuthResult>> HandleAsync(AcceptInviteCommand command, CancellationToken cancellationToken = default)
        {
            var codeOrToken = command.CodeOrToken.Trim();
            var invite = await _context.TenantInvites
                .SingleOrDefaultAsync(i => i.Token == codeOrToken || i.Code == codeOrToken, cancellationToken);

            if (invite == null || invite.UsedAtUtc != null || invite.ExpiresAtUtc < DateTime.UtcNow)
                return CommandResponse<AuthResult>.Error(new OutcomeEntry(nameof(command.CodeOrToken), "{0} is invalid or expired", attemptedValue: null, Severity.Error, GenericErrorCodes.ValidationFailed));

            var email = command.Email?.Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(invite.Email) &&
                !string.Equals(invite.Email, email, StringComparison.OrdinalIgnoreCase))
                return CommandResponse<AuthResult>.Error(new OutcomeEntry(nameof(command.Email), "{0} does not match the invite email", attemptedValue: null, Severity.Error, GenericErrorCodes.ValidationFailed));

            var existing = await _context.Users.AsNoTracking()
                .SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (existing != null)
                return CommandResponse<AuthResult>.Error(new OutcomeEntry(nameof(command.Email), "{0} is already registered", attemptedValue: null, Severity.Error, AuthErrorCodes.EmailAlreadyExists));

            var user = new Domain.User
            {
                Name = command.Name?.Trim(),
                Surname = command.Surname?.Trim(),
                Email = email,
                TenantId = invite.TenantId,
                TenantRole = TenantMemberRoles.Member,
                IsActive = true
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
            var authResult = await _authResultBuilder.BuildAsync(user, token, cancellationToken);
            return CommandResponse<AuthResult>.Success(authResult);
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
