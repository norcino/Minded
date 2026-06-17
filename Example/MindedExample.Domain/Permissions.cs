using System.Collections.Generic;

namespace MindedExample.Domain
{
    /// <summary>
    /// Centralized permission constants used in authorization attributes and join tables.
    /// Permissions are grouped by domain area for easier management in the admin UI.
    /// </summary>
    public static class Permissions
    {
        // Category permissions
        public const string CanCreateCategory = nameof(CanCreateCategory);
        public const string CanCreateRootCategory = nameof(CanCreateRootCategory);
        public const string CanUpdateCategory = nameof(CanUpdateCategory);
        public const string CanDeleteCategory = nameof(CanDeleteCategory);

        // Transaction permissions
        public const string CanCreateTransaction = nameof(CanCreateTransaction);
        public const string CanUpdateTransaction = nameof(CanUpdateTransaction);
        public const string CanDeleteTransaction = nameof(CanDeleteTransaction);

        // User permissions
        public const string CanCreateUser = nameof(CanCreateUser);
        public const string CanUpdateUser = nameof(CanUpdateUser);
        public const string CanDeleteUser = nameof(CanDeleteUser);

        // Role permissions
        public const string CanCreateRole = nameof(CanCreateRole);
        public const string CanDeleteRole = nameof(CanDeleteRole);
        public const string CanUpdateRolePermissions = nameof(CanUpdateRolePermissions);

        // Admin permissions
        public const string CanManageRoles = nameof(CanManageRoles);
        public const string CanAssignRoles = nameof(CanAssignRoles);

        // Configuration permissions
        public const string CanUpdateConfiguration = nameof(CanUpdateConfiguration);

        /// <summary>
        /// Permissions that cannot be removed from the Admin role.
        /// These are essential for role management and system administration.
        /// </summary>
        public static readonly string[] ProtectedAdminPermissions =
        [
            CanManageRoles, CanAssignRoles, CanCreateRole, CanDeleteRole, CanUpdateRolePermissions
        ];

        /// <summary>
        /// Permission groups for UI display and management.
        /// Each group maps a human-readable category label to its permission names.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string[]> Groups = new Dictionary<string, string[]>
        {
            ["Categories"] = [CanCreateCategory, CanCreateRootCategory, CanUpdateCategory, CanDeleteCategory],
            ["Transactions"] = [CanCreateTransaction, CanUpdateTransaction, CanDeleteTransaction],
            ["Users"] = [CanCreateUser, CanUpdateUser, CanDeleteUser],
            ["Roles"] = [CanCreateRole, CanDeleteRole, CanUpdateRolePermissions],
            ["Administration"] = [CanManageRoles, CanAssignRoles, CanUpdateConfiguration]
        };
    }
}
