using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Minded.Extensions.Authorization;
using MindedExample.Infrastructure.Persistence;

namespace MindedExample.Api.Authorization
{
    /// <summary>
    /// Authorization context accessor for the authenticated user.
    /// Resolves roles, permissions, and selected claims from persistence and request identity.
    /// </summary>
    public class CurrentUserAuthorizationContextAccessor : IAuthorizationContextAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public CurrentUserAuthorizationContextAccessor(
            IHttpContextAccessor httpContextAccessor,
            ICurrentUserAccessor currentUserAccessor,
            IMindedExampleContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _currentUserAccessor = currentUserAccessor;
            _context = context;
        }

        public AuthorizationContext Current
        {
            get
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    return new AuthorizationContext(false);
                }

                // Check if we already computed the context for this request
                if (httpContext.Items.TryGetValue("AuthorizationContext", out var cached) && cached is AuthorizationContext authCtx)
                {
                    return authCtx;
                }

                var userId = _currentUserAccessor.UserId;
                var tenantId = _currentUserAccessor.TenantId;

                if (!userId.HasValue)
                {
                    var noAuth = new AuthorizationContext(false);
                    httpContext.Items["AuthorizationContext"] = noAuth;
                    return noAuth;
                }

                var user = _context.Users
                    .AsNoTracking()
                    .SingleOrDefault(u => u.Id == userId.Value);

                if (user == null)
                {
                    var noAuth = new AuthorizationContext(false);
                    httpContext.Items["AuthorizationContext"] = noAuth;
                    return noAuth;
                }

                var effectiveTenantId = tenantId ?? user.TenantId;

                var roles = new List<string>();
                var permissions = new List<string>();

                if (_context is MindedExampleContext concreteContext)
                {
                    // Query UserRoles for this user's role names
                    roles = concreteContext.Set<Dictionary<string, object>>("UserRoles")
                        .Where(ur => (int)ur["TenantId"] == effectiveTenantId && (int)ur["UserId"] == userId.Value)
                        .Select(ur => (string)ur["RoleName"])
                        .ToList();

                    if (roles.Count > 0)
                    {
                        // Query RolePermissions for those roles' permission names
                        permissions = concreteContext.Set<Dictionary<string, object>>("RolePermissions")
                            .Where(rp => (int)rp["TenantId"] == effectiveTenantId)
                            .ToList()
                            .Where(rp => roles.Contains((string)rp["RoleName"]))
                            .Select(rp => (string)rp["PermissionName"])
                            .Distinct()
                            .ToList();
                    }
                }

                var claims = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["is_global_admin"] = _currentUserAccessor.IsGlobalAdmin.ToString().ToLowerInvariant()
                };

                if (effectiveTenantId.HasValue)
                {
                    claims["tenant_id"] = effectiveTenantId.Value.ToString();
                }

                if (userId.HasValue)
                {
                    claims["sub"] = userId.Value.ToString();
                }

                var result = new AuthorizationContext(true, roles, permissions, claims);
                httpContext.Items["AuthorizationContext"] = result;
                return result;
            }
        }
    }
}
