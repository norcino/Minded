using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.User.Command
{
    /// <summary>
    /// Command to delete a user by ID within the current tenant.
    /// </summary>
    [ValidateCommand]
    public class DeleteUserCommand : ICommand, ILoggable
    {
        /// <summary>Gets the ID of the user to delete.</summary>
        public int UserId { get; }

        /// <inheritdoc />
        public Guid TraceId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public string LoggingTemplate => "UserId: {UserId}";

        /// <inheritdoc />
        public string[] LoggingProperties => [nameof(UserId)];

        /// <summary>
        /// Initializes a new <see cref="DeleteUserCommand"/>.
        /// </summary>
        public DeleteUserCommand(int userId, Guid? traceId = null)
        {
            UserId = userId;
            TraceId = traceId ?? TraceId;
        }
    }
}