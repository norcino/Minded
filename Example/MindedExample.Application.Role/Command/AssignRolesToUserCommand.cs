using System;
using System.Collections.Generic;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Extensions.Authorization.Attributes;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.Role.Command
{
    /// <summary>
    /// Command to set the roles for a user. Replaces all existing role assignments.
    /// </summary>
    [ValidateCommand]
    [RequirePermissions(MindedExample.Domain.Permissions.CanAssignRoles)]
    public class AssignRolesToUserCommand : ICommand, ILoggable
    {
        public int UserId { get; }
        public List<string> RoleNames { get; }

        public AssignRolesToUserCommand(int userId, List<string> roleNames, Guid? traceId = null)
        {
            UserId = userId;
            RoleNames = roleNames;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();
        public string LoggingTemplate => "UserId: {UserId} RoleCount: {RoleCount}";
        public string[] LoggingProperties => [nameof(UserId), "RoleNames.Count"];
    }
}
