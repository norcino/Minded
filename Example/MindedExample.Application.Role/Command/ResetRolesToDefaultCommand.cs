using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Authorization.Attributes;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.Role.Command
{
    [RequirePermissions(MindedExample.Domain.Permissions.CanManageRoles)]
    public class ResetRolesToDefaultCommand : ICommand, ILoggable
    {
        public ResetRolesToDefaultCommand(Guid? traceId = null)
        {
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();
        public string LoggingTemplate => "ResetRolesToDefault";
        public string[] LoggingProperties => [];
    }
}
