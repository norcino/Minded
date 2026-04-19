using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Minded.Extensions.Authorization;
using MindedExample.Infrastructure.Persistence;

namespace MindedExample.Api.Authorization
{
    /// <summary>
    /// Authorization context accessor that reads the impersonated user from the X-Impersonate-UserId header.
    /// Resolves the user's roles and permissions from the database to build the authorization context.
    /// </summary>
    public class ImpersonationAuthorizationContextAccessor : IAuthorizationContextAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMindedExampleContext _context;

        public ImpersonationAuthorizationContextAccessor(IHttpContextAccessor httpContextAccessor, IMindedExampleContext context)
        {
            _httpContextAccessor = httpContextAccessor;
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

                var userIdHeader = httpContext.Request.Headers["X-Impersonate-UserId"].FirstOrDefault();
                if (string.IsNullOrEmpty(userIdHeader) || !int.TryParse(userIdHeader, out int userId))
                {
                    var noAuth = new AuthorizationContext(false);
                    httpContext.Items["AuthorizationContext"] = noAuth;
                    return noAuth;
                }

                var user = _context.Users
                    .AsNoTracking()
                    .SingleOrDefault(u => u.Id == userId);

                if (user == null)
                {
                    var noAuth = new AuthorizationContext(false);
                    httpContext.Items["AuthorizationContext"] = noAuth;
                    return noAuth;
                }

                var roles = new List<string>();
                var permissions = new List<string>();

                if (_context is MindedExampleContext concreteContext)
                {
                    // Query UserRoles for this user's role names
                    roles = concreteContext.Set<Dictionary<string, object>>("UserRoles")
                        .Where(ur => (int)ur["UserId"] == userId)
                        .Select(ur => (string)ur["RoleName"])
                        .ToList();

                    if (roles.Count > 0)
                    {
                        // Query RolePermissions for those roles' permission names
                        permissions = concreteContext.Set<Dictionary<string, object>>("RolePermissions")
                            .ToList()
                            .Where(rp => roles.Contains((string)rp["RoleName"]))
                            .Select(rp => (string)rp["PermissionName"])
                            .Distinct()
                            .ToList();
                    }
                }

                var result = new AuthorizationContext(true, roles, permissions);
                httpContext.Items["AuthorizationContext"] = result;
                return result;
            }
        }
    }
}
