using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.User.Command
{
    /// <summary>
    /// Command to update an existing user within the current tenant.
    /// </summary>
    [ValidateCommand]
    public class UpdateUserCommand : ICommand, ILoggable
    {
        /// <summary>Gets the ID of the user to update.</summary>
        public int UserId { get; }

        /// <summary>Gets the updated user data.</summary>
        public MindedExample.Domain.User User { get; }

        /// <inheritdoc />
        public Guid TraceId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public string LoggingTemplate => "UserId: {UserId}";

        /// <inheritdoc />
        public string[] LoggingProperties => [nameof(UserId)];

        /// <summary>
        /// Initializes a new <see cref="UpdateUserCommand"/>.
        /// </summary>
        public UpdateUserCommand(int userId, MindedExample.Domain.User user, Guid? traceId = null)
        {
            UserId = userId;
            User = user;
            TraceId = traceId ?? TraceId;
        }
    }
}