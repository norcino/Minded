using System;

namespace Minded.Extensions.Authorization.Attributes
{
    /// <summary>
    /// Declares that the command or query requires the caller to possess the specified permissions.
    /// Multiple instances on the same class are combined with implicit AND.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequirePermissionsAttribute : Attribute
    {
        /// <summary>Gets the permission names required by this clause.</summary>
        public string[] Permissions { get; }

        /// <summary>Gets or sets how the permissions are matched. Default is <see cref="AuthorizationMatch.All"/>.</summary>
        public new AuthorizationMatch Match { get; set; } = AuthorizationMatch.All;

        /// <summary>Gets or sets the minimum number of matching permissions when <see cref="Match"/> is <see cref="AuthorizationMatch.AtLeast"/>. Default is 0.</summary>
        public int Minimum { get; set; } = 0;

        /// <summary>
        /// Gets or sets role names that short-circuit this clause when any one is present on the caller.
        /// </summary>
        public string[] OrAnyRole { get; set; }

        /// <summary>
        /// Gets or sets permission names that short-circuit this clause when any one is present on the caller.
        /// </summary>
        public string[] OrAnyPermission { get; set; }

        /// <summary>
        /// Gets or sets claim keys that short-circuit this clause when any one is present on the caller.
        /// </summary>
        public string[] OrAnyClaim { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="RequirePermissionsAttribute"/> with the specified permission names.
        /// </summary>
        /// <param name="permissions">The permission names required by this clause.</param>
        public RequirePermissionsAttribute(params string[] permissions)
        {
            Permissions = permissions;
        }
    }
}
