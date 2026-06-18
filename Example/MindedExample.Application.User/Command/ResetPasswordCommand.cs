using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.User.Command
{
    /// <summary>
    /// Command to complete a password reset using a previously issued reset token.
    /// Returns HTTP 400 Bad Request if the token is invalid or already used.
    /// </summary>
    [ValidateCommand]
    public class ResetPasswordCommand : ICommand, ILoggable
    {
        /// <summary>Gets the password reset token received by the user.</summary>
        public string Token { get; }

        /// <summary>Gets the new plain-text password to set.</summary>
        public string NewPassword { get; }

        /// <inheritdoc />
        public Guid TraceId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public string LoggingTemplate => "ResetPassword with token";

        /// <inheritdoc />
        public string[] LoggingProperties => [];

        /// <summary>
        /// Initializes a new <see cref="ResetPasswordCommand"/>.
        /// </summary>
        public ResetPasswordCommand(string token, string newPassword, Guid? traceId = null)
        {
            Token = token;
            NewPassword = newPassword;
            TraceId = traceId ?? TraceId;
        }
    }
}
