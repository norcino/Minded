using System;

namespace MindedExample.Application.User.Command
{
    /// <summary>
    /// Represents the result of a successful tenant invite creation,
    /// including the shareable invite link.
    /// </summary>
    public class TenantInviteResult
    {
        /// <summary>Gets or sets the database identifier of the created invite.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the optional pre-assigned email address.</summary>
        public string Email { get; set; }

        /// <summary>Gets or sets the short alphanumeric redemption code.</summary>
        public string Code { get; set; }

        /// <summary>Gets or sets the secure token used to build the invite link.</summary>
        public string Token { get; set; }

        /// <summary>Gets or sets the fully-qualified invite link that can be shared with the invitee.</summary>
        public string InviteLink { get; set; }

        /// <summary>Gets or sets the UTC expiry time in ISO 8601 round-trip format.</summary>
        public string ExpiresAtUtc { get; set; }
    }
}
