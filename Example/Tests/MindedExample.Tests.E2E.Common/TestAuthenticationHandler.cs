using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MindedExample.Tests.E2E.Common
{
    /// <summary>
    /// Identity used by <see cref="TestAuthenticationHandler"/> for every request.
    /// BaseE2ETest sets these values during test initialization, once the baseline
    /// tenant and user have been created in the test database.
    /// </summary>
    internal static class TestAuthenticationState
    {
        public static int UserId { get; set; }
        public static int? TenantId { get; set; }
        public static string TenantRole { get; set; } = MindedExample.Domain.TenantMemberRoles.Owner;
        public static bool IsGlobalAdmin { get; set; }
        public static string Email { get; set; } = "e2e-admin@example.com";
    }

    /// <summary>
    /// Authenticates every request as the baseline test user, mirroring the claims issued
    /// by JwtTokenFactory so authorization policies and controllers behave as in production.
    /// </summary>
    internal class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "TestAuth";

        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>
            {
                new Claim("sub", TestAuthenticationState.UserId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, TestAuthenticationState.UserId.ToString()),
                new Claim(ClaimTypes.Email, TestAuthenticationState.Email),
                new Claim("tenant_role", TestAuthenticationState.TenantRole),
                new Claim("is_global_admin", TestAuthenticationState.IsGlobalAdmin.ToString().ToLowerInvariant())
            };

            if (TestAuthenticationState.TenantId.HasValue)
            {
                claims.Add(new Claim("tenant_id", TestAuthenticationState.TenantId.Value.ToString()));
            }

            var identity = new ClaimsIdentity(claims, SchemeName);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
