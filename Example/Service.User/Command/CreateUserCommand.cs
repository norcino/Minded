using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace Service.User.Command
{
    /// <summary>
    /// Command to create a new user.
    /// This command is validated before execution.
    /// User's sensitive data (name, surname, email) will be protected in logs by the DataProtectionLoggingSanitizer.
    /// </summary>
    [ValidateCommand]
    public class CreateUserCommand : ICommand<Data.Entity.User>, ILoggable
    {
        public Data.Entity.User User { get; set; }

        public CreateUserCommand(Data.Entity.User user, Guid? traceId = null)
        {
            User = user;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "Creating user with email: {Email} - name: {Name}, surname: {Surname}";

        public string[] LoggingProperties => new[] { "User.Email", "User.Name", "User.Surname" };
    }
}

