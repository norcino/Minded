using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace Service.User.Command
{
    /// <summary>
    /// Command to update an existing user.
    /// Implements ILoggable for automatic logging through the logging decorator.
    /// Uses ValidateCommand attribute to ensure the user exists before update.
    /// User's sensitive data (name, surname, email) will be protected in logs by the DataProtectionLoggingSanitizer.
    /// </summary>
    [ValidateCommand]
    public class UpdateUserCommand : ICommand, ILoggable
    {
        public Data.Entity.User User { get; }
        public int UserId { get; }

        public UpdateUserCommand(int id, Data.Entity.User user, Guid? traceId = null)
        {
            UserId = id;
            User = user;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "UserId: {UserId}";

        public string[] LoggingProperties => new[] { "User.Id" };
    }
}

