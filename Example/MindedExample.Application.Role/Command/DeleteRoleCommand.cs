using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Extensions.Authorization.Attributes;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.Role.Command
{
    [ValidateCommand]
    [RequirePermissions(MindedExample.Domain.Permissions.CanDeleteRole)]
    public class DeleteRoleCommand : ICommand, ILoggable
    {
        public string RoleName { get; }

        public DeleteRoleCommand(string roleName, Guid? traceId = null)
        {
            RoleName = roleName;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();
        public string LoggingTemplate => "RoleName: {RoleName}";
        public string[] LoggingProperties => [nameof(RoleName)];
    }
}
