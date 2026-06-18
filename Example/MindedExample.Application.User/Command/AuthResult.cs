using System.Collections.Generic;

namespace MindedExample.Application.User.Command
{
    /// <summary>
    /// Result DTO returned by authentication commands (Register, Login, AcceptInvite)
    /// and the GetCurrentUser query. Property names intentionally match those of
    /// <c>MindedExample.Api.Models.AuthResponse</c> so that JSON serialization is compatible
    /// with the existing API models and E2E test deserializers.
    /// </summary>
    public class AuthResult
    {
        /// <summary>Gets or sets the JWT access token. Null for the /me query response.</summary>
        public string AccessToken { get; set; }

        /// <summary>Gets or sets the authenticated user profile.</summary>
        public AuthUserResult User { get; set; }

        /// <summary>Gets or sets the tenant the user belongs to.</summary>
        public TenantResult Tenant { get; set; }

        /// <summary>
        /// Set to <c>true</c> for the join-tenant registration flow,
        /// indicating the request is pending admin approval.
        /// </summary>
        public bool? PendingApproval { get; set; }

        /// <summary>Optional message accompanying a pending-approval response.</summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// Authenticated user profile returned as part of <see cref="AuthResult"/>.
    /// </summary>
    public class AuthUserResult
    {
        public int Id { get; set; }
        public int? TenantId { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string TenantRole { get; set; }
        public bool IsGlobalAdmin { get; set; }
        public IReadOnlyCollection<string> Roles { get; set; }
    }

    /// <summary>
    /// Tenant summary returned as part of <see cref="AuthResult"/>.
    /// </summary>
    public class TenantResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
