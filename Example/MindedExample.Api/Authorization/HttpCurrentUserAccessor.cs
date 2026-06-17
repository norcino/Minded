using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MindedExample.Infrastructure.Persistence;

namespace MindedExample.Api.Authorization
{
    /// <summary>
    /// Resolves current user and tenant from JWT claims.
    /// </summary>
    public class HttpCurrentUserAccessor : ICurrentUserAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? UserId
        {
            get
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    return null;
                }

                var subject = httpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User?.FindFirstValue("sub");

                if (int.TryParse(subject, out var userId))
                {
                    return userId;
                }

                return null;
            }
        }

        public int? TenantId
        {
            get
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    return null;
                }

                var tenant = httpContext.User?.FindFirstValue("tenant_id");
                if (int.TryParse(tenant, out var tenantId))
                {
                    return tenantId;
                }

                return null;
            }
        }

        public bool IsGlobalAdmin
        {
            get
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    return false;
                }

                var claim = httpContext.User?.FindFirstValue("is_global_admin");
                return bool.TryParse(claim, out var result) && result;
            }
        }
    }
}
