using System.Collections.Generic;
using System.Linq;

namespace MindedExample.Domain
{
    /// <summary>
    /// Defines the predefined roles and their default permission assignments.
    /// Used by the database seeder and the "Reset Roles to Default" admin feature.
    /// </summary>
    public static class DefaultRolesDefinition
    {
        /// <summary>
        /// The role assigned to newly registered users.
        /// </summary>
        public const string DefaultRole = Roles.User;

        /// <summary>
        /// Default permission assignments for each predefined role.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string[]> RolePermissions = new Dictionary<string, string[]>
        {
            [Roles.Admin] = new[]
            {
                Permissions.CanCreateCategory, Permissions.CanCreateRootCategory,
                Permissions.CanUpdateCategory, Permissions.CanDeleteCategory,
                Permissions.CanCreateTransaction, Permissions.CanUpdateTransaction,
                Permissions.CanDeleteTransaction, Permissions.CanCreateUser,
                Permissions.CanUpdateUser, Permissions.CanDeleteUser,
                Permissions.CanManageRoles, Permissions.CanAssignRoles,
                Permissions.CanCreateRole, Permissions.CanDeleteRole,
                Permissions.CanUpdateRolePermissions, Permissions.CanUpdateConfiguration
            },
            [Roles.TenantAdmin] = new[]
            {
                Permissions.CanCreateCategory, Permissions.CanCreateRootCategory,
                Permissions.CanUpdateCategory, Permissions.CanDeleteCategory,
                Permissions.CanCreateTransaction, Permissions.CanUpdateTransaction,
                Permissions.CanDeleteTransaction, Permissions.CanCreateUser,
                Permissions.CanUpdateUser, Permissions.CanDeleteUser
            },
            [Roles.User] = new[]
            {
                Permissions.CanCreateTransaction,
                Permissions.CanUpdateTransaction,
                Permissions.CanDeleteTransaction
            }
        };

        /// <summary>
        /// Returns all predefined role names.
        /// </summary>
        public static IEnumerable<string> AllRoles => RolePermissions.Keys;

        /// <summary>
        /// Returns all known permission names derived from Permissions.Groups.
        /// </summary>
        public static readonly string[] AllPermissions =
            Permissions.Groups.Values.SelectMany(p => p).ToArray();
    }
}
