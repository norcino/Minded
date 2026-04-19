using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Authorization.Attributes;
using MindedExample.Application.Role.Command;
using MindedExample.Domain;

namespace MindedExample.Application.Role.UnitTests
{
    [TestClass]
    public class RoleCommandPermissionAttributeTests
    {
        [TestMethod]
        public void CreateRoleCommand_RequiresCanCreateRolePermission()
        {
            var attr = typeof(CreateRoleCommand)
                .GetCustomAttribute<RequirePermissionsAttribute>();

            attr.Should().NotBeNull();
            attr.Permissions.Should().Contain(Permissions.CanCreateRole);
        }

        [TestMethod]
        public void DeleteRoleCommand_RequiresCanDeleteRolePermission()
        {
            var attr = typeof(DeleteRoleCommand)
                .GetCustomAttribute<RequirePermissionsAttribute>();

            attr.Should().NotBeNull();
            attr.Permissions.Should().Contain(Permissions.CanDeleteRole);
        }

        [TestMethod]
        public void UpdateRolePermissionsCommand_RequiresCanUpdateRolePermissionsPermission()
        {
            var attr = typeof(UpdateRolePermissionsCommand)
                .GetCustomAttribute<RequirePermissionsAttribute>();

            attr.Should().NotBeNull();
            attr.Permissions.Should().Contain(Permissions.CanUpdateRolePermissions);
        }

        [TestMethod]
        public void AssignRolesToUserCommand_RequiresCanAssignRolesPermission()
        {
            var attr = typeof(AssignRolesToUserCommand)
                .GetCustomAttribute<RequirePermissionsAttribute>();

            attr.Should().NotBeNull();
            attr.Permissions.Should().Contain(Permissions.CanAssignRoles);
        }

        [TestMethod]
        public void ResetRolesToDefaultCommand_RequiresCanManageRolesPermission()
        {
            var attr = typeof(ResetRolesToDefaultCommand)
                .GetCustomAttribute<RequirePermissionsAttribute>();

            attr.Should().NotBeNull();
            attr.Permissions.Should().Contain(Permissions.CanManageRoles);
        }

        [TestMethod]
        public void AdminRole_HasAllProtectedPermissions()
        {
            var adminPermissions = DefaultRolesDefinition.RolePermissions[Roles.Admin];

            foreach (var protectedPerm in Permissions.ProtectedAdminPermissions)
            {
                adminPermissions.Should().Contain(protectedPerm,
                    $"Admin role must have protected permission '{protectedPerm}'");
            }
        }

        [TestMethod]
        public void AdminRole_HasAllNewPermissions()
        {
            var adminPermissions = DefaultRolesDefinition.RolePermissions[Roles.Admin];

            adminPermissions.Should().Contain(Permissions.CanCreateRole);
            adminPermissions.Should().Contain(Permissions.CanDeleteRole);
            adminPermissions.Should().Contain(Permissions.CanUpdateRolePermissions);
            adminPermissions.Should().Contain(Permissions.CanUpdateConfiguration);
        }
    }
}
