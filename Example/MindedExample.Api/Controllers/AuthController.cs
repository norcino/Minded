using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MindedExample.Api.Authorization;
using MindedExample.Api.Models;
using MindedExample.Domain;
using MindedExample.Infrastructure.Persistence;

namespace MindedExample.Api.Controllers
{
    /// <summary>
    /// Provides authentication, registration, invitation onboarding, and password reset endpoints.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private const string RegisterModeCreateTenant = "create-tenant";
        private const string RegisterModeJoinTenant = "join-tenant";

        private readonly IMindedExampleContext _context;
        private readonly JwtTokenFactory _jwtTokenFactory;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IMindedExampleContext context,
            JwtTokenFactory jwtTokenFactory,
            ICurrentUserAccessor currentUserAccessor,
            IPasswordHasher<User> passwordHasher,
            ILogger<AuthController> logger)
        {
            _context = context;
            _jwtTokenFactory = jwtTokenFactory;
            _currentUserAccessor = currentUserAccessor;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request?.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Email and password are required.");
            }

            var email = request.Email.Trim().ToLowerInvariant();
            var existing = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (existing != null)
            {
                return Conflict("A user with this email already exists.");
            }

            if (await _context.TenantJoinRequests.AsNoTracking().AnyAsync(
                    r => r.Email == email && r.ProcessedAtUtc == null,
                    cancellationToken))
            {
                return Conflict("A pending join request with this email already exists.");
            }

            if (!string.IsNullOrWhiteSpace(request.InviteToken))
            {
                return await RegisterFromInviteAsync(request, email, cancellationToken);
            }

            var mode = string.IsNullOrWhiteSpace(request.Mode)
                ? RegisterModeCreateTenant
                : request.Mode.Trim().ToLowerInvariant();

            if (mode == RegisterModeJoinTenant)
            {
                return await RegisterJoinTenantRequestAsync(request, email, cancellationToken);
            }

            return await RegisterCreateTenantAsync(request, email, cancellationToken);
        }

        [HttpGet("invite/{token}")]
        public async Task<IActionResult> GetInviteDetails(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest("Invite token is required.");
            }

            var invite = await _context.TenantInvites
                .Include(i => i.Tenant)
                .AsNoTracking()
                .SingleOrDefaultAsync(i => i.Token == token || i.Code == token, cancellationToken);

            if (invite == null || invite.UsedAtUtc != null || invite.ExpiresAtUtc < DateTime.UtcNow)
            {
                return NotFound();
            }

            return Ok(new InviteResolutionDto
            {
                TenantName = invite.Tenant?.Name,
                Email = invite.Email
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request?.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Email and password are required.");
            }

            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return Unauthorized("Invalid credentials.");
            }

            if (!user.IsActive)
            {
                var hasPendingRequest = await _context.TenantJoinRequests.AsNoTracking().AnyAsync(
                    r => r.Email == email && r.ProcessedAtUtc == null,
                    cancellationToken);

                if (hasPendingRequest)
                {
                    return StatusCode(403, "Your request to join the tenant is pending approval.");
                }

                return Unauthorized("Invalid credentials.");
            }

            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verify == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Invalid credentials.");
            }

            var token = _jwtTokenFactory.CreateToken(user);
            return Ok(await BuildAuthResponseAsync(user, token, cancellationToken));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request?.Email))
            {
                return Ok();
            }

            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);
            if (user == null)
            {
                return Ok();
            }

            var token = Guid.NewGuid().ToString("N");
            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(2)
            });

            await _context.SaveChangesAsync(cancellationToken);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var resetLink = $"{baseUrl}/reset-password?token={token}";
            _logger.LogInformation("Password reset link for {Email}: {ResetLink}", user.Email, resetLink);

            return Ok();
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request?.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest("Token and new password are required.");
            }

            var token = await _context.PasswordResetTokens
                .Include(t => t.User)
                .SingleOrDefaultAsync(t => t.Token == request.Token, cancellationToken);

            if (token == null || token.UsedAtUtc != null || token.ExpiresAtUtc < DateTime.UtcNow)
            {
                return BadRequest("Invalid or expired token.");
            }

            token.User.PasswordHash = _passwordHasher.HashPassword(token.User, request.NewPassword);
            token.UsedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return Ok();
        }

        [HttpPost("accept-invite")]
        public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request?.CodeOrToken) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Invite code/link and password are required.");
            }

            var codeOrToken = request.CodeOrToken.Trim();
            var invite = await _context.TenantInvites
                .SingleOrDefaultAsync(i => i.Token == codeOrToken || i.Code == codeOrToken, cancellationToken);

            if (invite == null || invite.UsedAtUtc != null || invite.ExpiresAtUtc < DateTime.UtcNow)
            {
                return BadRequest("Invite is invalid or expired.");
            }

            var email = request.Email?.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(invite.Email) && !string.Equals(invite.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Invite email does not match.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Email is required.");
            }

            var existing = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (existing != null)
            {
                return Conflict("A user with this email already exists.");
            }

            var user = new User
            {
                Name = request.Name?.Trim(),
                Surname = request.Surname?.Trim(),
                Email = email,
                TenantId = invite.TenantId,
                TenantRole = TenantMemberRoles.Member,
                IsActive = true
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            invite.UsedAtUtc = DateTime.UtcNow;
            invite.UsedByUserId = user.Id;

            await EnsureTenantRolePermissionsInitializedAsync(invite.TenantId);
            await AssignUserRoleAsync(invite.TenantId, user.Id, Roles.User);

            await _context.SaveChangesAsync(cancellationToken);

            var token = _jwtTokenFactory.CreateToken(user);
            return Ok(await BuildAuthResponseAsync(user, token, cancellationToken));
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me(CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.UserId.HasValue)
            {
                return Unauthorized();
            }

            var user = await _context.Users.SingleOrDefaultAsync(u => u.Id == _currentUserAccessor.UserId.Value, cancellationToken);
            if (user == null)
            {
                return Unauthorized();
            }

            return Ok(await BuildAuthResponseAsync(user, null, cancellationToken));
        }

        private async Task EnsureTenantRolePermissionsInitializedAsync(int tenantId)
        {
            if (_context is not MindedExampleContext concreteContext)
            {
                return;
            }

            var hasAny = await concreteContext.Set<Dictionary<string, object>>("RolePermissions")
                .AnyAsync(rp => (int)rp["TenantId"] == tenantId);
            if (hasAny)
            {
                return;
            }

            // Inserted through the shared-type entity set (not raw SQL) so EF generates
            // correctly quoted, schema-qualified SQL for every database provider.
            var rolePermissions = concreteContext.Set<Dictionary<string, object>>("RolePermissions");
            foreach (var kvp in DefaultRolesDefinition.RolePermissions)
            {
                foreach (var permission in kvp.Value)
                {
                    rolePermissions.Add(new Dictionary<string, object>
                    {
                        ["TenantId"] = tenantId,
                        ["RoleName"] = kvp.Key,
                        ["PermissionName"] = permission
                    });
                }
            }

            await concreteContext.SaveChangesAsync();
        }

        private async Task<AuthResponse> BuildAuthResponseAsync(User user, string token, CancellationToken cancellationToken)
        {
            var tenant = user.TenantId.HasValue
                ? await _context.Tenants.AsNoTracking().SingleOrDefaultAsync(t => t.Id == user.TenantId.Value, cancellationToken)
                : null;

            var roles = new List<string>();

            if (user.IsGlobalAdmin)
            {
                roles.Add(Roles.Admin);
            }

            if (_context is MindedExampleContext concreteContext && user.TenantId.HasValue)
            {
                roles = await concreteContext.Set<Dictionary<string, object>>("UserRoles")
                    .Where(ur => (int)ur["TenantId"] == user.TenantId.Value && (int)ur["UserId"] == user.Id)
                    .Select(ur => (string)ur["RoleName"])
                    .ToListAsync(cancellationToken);
            }

            return new AuthResponse
            {
                AccessToken = token,
                User = new AuthUserDto
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
                Tenant = tenant == null ? null : new TenantDto { Id = tenant.Id, Name = tenant.Name }
            };
        }

        private async Task<IActionResult> RegisterCreateTenantAsync(RegisterRequest request, string email, CancellationToken cancellationToken)
        {
            var tenant = new Tenant
            {
                Name = string.IsNullOrWhiteSpace(request.TenantName)
                    ? $"{request.Name} {request.Surname}".Trim() + " Tenant"
                    : request.TenantName.Trim()
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync(cancellationToken);

            var user = new User
            {
                Name = request.Name?.Trim(),
                Surname = request.Surname?.Trim(),
                Email = email,
                TenantId = tenant.Id,
                TenantRole = TenantMemberRoles.Owner,
                IsActive = true,
                IsGlobalAdmin = false
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            tenant.LegalOwnerUserId = user.Id;
            await _context.SaveChangesAsync(cancellationToken);

            await EnsureTenantRolePermissionsInitializedAsync(tenant.Id);
            await AssignUserRoleAsync(tenant.Id, user.Id, Roles.TenantAdmin);

            var token = _jwtTokenFactory.CreateToken(user);
            return Ok(await BuildAuthResponseAsync(user, token, cancellationToken));
        }

        private async Task<IActionResult> RegisterJoinTenantRequestAsync(RegisterRequest request, string email, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.TenantName))
            {
                return BadRequest("Tenant name is required.");
            }

            var tenantName = request.TenantName.Trim();
            var tenant = await _context.Tenants.SingleOrDefaultAsync(t => t.Name == tenantName, cancellationToken);
            if (tenant == null)
            {
                return NotFound("Tenant not found.");
            }

            _context.TenantJoinRequests.Add(new TenantJoinRequest
            {
                TenantId = tenant.Id,
                Name = request.Name?.Trim(),
                Surname = request.Surname?.Trim(),
                Email = email,
                PasswordHash = _passwordHasher.HashPassword(new User { Email = email }, request.Password),
                CreatedAtUtc = DateTime.UtcNow,
                Approved = false
            });

            await _context.SaveChangesAsync(cancellationToken);
            return Ok(new
            {
                PendingApproval = true,
                Message = "Registration completed. A tenant administrator must approve your request before you can log in."
            });
        }

        private async Task<IActionResult> RegisterFromInviteAsync(RegisterRequest request, string email, CancellationToken cancellationToken)
        {
            var tokenOrCode = request.InviteToken.Trim();
            var invite = await _context.TenantInvites
                .SingleOrDefaultAsync(i => i.Token == tokenOrCode || i.Code == tokenOrCode, cancellationToken);

            if (invite == null || invite.UsedAtUtc != null || invite.ExpiresAtUtc < DateTime.UtcNow)
            {
                return BadRequest("Invite is invalid or expired.");
            }

            if (!string.IsNullOrWhiteSpace(invite.Email) &&
                !string.Equals(invite.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Invite email does not match.");
            }

            var user = new User
            {
                Name = request.Name?.Trim(),
                Surname = request.Surname?.Trim(),
                Email = email,
                TenantId = invite.TenantId,
                TenantRole = TenantMemberRoles.Member,
                IsActive = true,
                IsGlobalAdmin = false
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            invite.UsedAtUtc = DateTime.UtcNow;
            invite.UsedByUserId = user.Id;

            await EnsureTenantRolePermissionsInitializedAsync(invite.TenantId);
            await AssignUserRoleAsync(invite.TenantId, user.Id, Roles.User);
            await _context.SaveChangesAsync(cancellationToken);

            var token = _jwtTokenFactory.CreateToken(user);
            return Ok(await BuildAuthResponseAsync(user, token, cancellationToken));
        }

        private async Task AssignUserRoleAsync(int tenantId, int userId, string roleName)
        {
            if (_context is not MindedExampleContext concreteContext)
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
