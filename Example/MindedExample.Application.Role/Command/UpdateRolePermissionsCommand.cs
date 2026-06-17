using System;
using System.Collections.Generic;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Extensions.Authorization.Attributes;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.Role.Command
{
    /// <summary>
    /// Command to set the permissions for a role. Replaces all existing permissions.
    /// </summary>
    [ValidateCommand]
    [RequirePermissions(MindedExample.Domain.Permissions.CanUpdateRolePermissions)]
    public class UpdateRolePermissionsCommand : ICommand, ILoggable
    {
        public string RoleName { get; }
        public List<string> PermissionNames { get; }

        public UpdateRolePermissionsCommand(string roleName, List<string> permissionNames, Guid? traceId = null)
        {
            RoleName = roleName;
            PermissionNames = permissionNames;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();
        public string LoggingTemplate => "RoleName: {RoleName} PermissionCount: {PermissionCount}";
        public string[] LoggingProperties => [nameof(RoleName), "PermissionNames.Count"];
    }
}
