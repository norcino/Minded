using System;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.User.Command
{
    /// <summary>
    /// Command to initiate a password reset flow.
    /// Creates a one-time reset token that is logged for delivery via the email infrastructure.
    /// Always returns success (HTTP 200 OK) regardless of whether the email is known,
    /// to avoid leaking information about account existence.
    /// </summary>
    public class ForgotPasswordCommand : ICommand, ILoggable
    {
        /// <summary>Gets the email address of the account requesting password reset.</summary>
        public string Email { get; }

        /// <inheritdoc />
        public Guid TraceId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public string LoggingTemplate => "ForgotPassword for {Email}";

        /// <inheritdoc />
        public string[] LoggingProperties => [nameof(Email)];

        /// <summary>
        /// Initializes a new <see cref="ForgotPasswordCommand"/>.
        /// </summary>
        public ForgotPasswordCommand(string email, Guid? traceId = null)
        {
            Email = email;
            TraceId = traceId ?? TraceId;
        }
    }
}
