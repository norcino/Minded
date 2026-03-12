using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace Service.User.Command
{
    /// <summary>
    /// Command to delete a user by ID.
    /// Implements ILoggable for automatic logging through the logging decorator.
    /// Uses ValidateCommand attribute to ensure the user exists before deletion.
    /// </summary>
    [ValidateCommand]
    public class DeleteUserCommand : ICommand, ILoggable
    {
        public int UserId { get; }

        public DeleteUserCommand(int id, Guid? traceId = null)
        {
            UserId = id;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "UserId: {UserId}";

        public string[] LoggingProperties => new[] { nameof(UserId) };
    }
}

