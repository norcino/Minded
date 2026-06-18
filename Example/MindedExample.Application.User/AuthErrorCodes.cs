namespace MindedExample.Application.User
{
    /// <summary>
    /// Error codes specific to authentication operations.
    /// These codes are used in <see cref="Minded.Framework.CQRS.Abstractions.OutcomeEntry"/> to signal
    /// business-level failures that map to specific HTTP status codes via the custom REST rules provider.
    /// </summary>
    public static class AuthErrorCodes
    {
        /// <summary>
        /// A user with the given email address already exists in the system.
        /// Maps to HTTP 409 Conflict.
        /// </summary>
        public const string EmailAlreadyExists = "email_already_exists";

        /// <summary>
        /// A pending join request with the given email already exists.
        /// Maps to HTTP 409 Conflict.
        /// </summary>
        public const string PendingJoinRequestExists = "pending_join_request_exists";
    }
}
