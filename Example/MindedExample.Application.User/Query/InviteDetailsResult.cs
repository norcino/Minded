namespace MindedExample.Application.User.Query
{
    /// <summary>
    /// Result DTO returned by <see cref="GetInviteDetailsQuery"/>.
    /// Contains the information needed for a prospective member to understand which
    /// tenant they are being invited to join. Property names match
    /// <c>MindedExample.Api.Models.InviteResolutionDto</c> for JSON compatibility.
    /// </summary>
    public class InviteDetailsResult
    {
        /// <summary>Gets or sets the name of the tenant the invite belongs to.</summary>
        public string TenantName { get; set; }

        /// <summary>Gets or sets the email address the invite was sent to, if any.</summary>
        public string Email { get; set; }
    }
}
