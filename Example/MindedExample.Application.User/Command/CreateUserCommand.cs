using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.User.Command
{
    /// <summary>
    /// Command to create a new user within the current tenant.
    /// Returns the created user on success.
    /// </summary>
    [ValidateCommand]
    public class CreateUserCommand : ICommand<MindedExample.Domain.User>, ILoggable
    {
        public MindedExample.Domain.User User { get; }
        public Guid TraceId { get; } = Guid.NewGuid();
        public string LoggingTemplate => "CreateUser";
        public string[] LoggingProperties => [];

        public CreateUserCommand(MindedExample.Domain.User user, Guid? traceId = null)
        {
            User = user;
            TraceId = traceId ?? TraceId;
        }
    }
}
