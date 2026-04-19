using System;

namespace Minded.Extensions.Authorization.Attributes
{
    /// <summary>
    /// Declares that the command or query requires the caller to possess the specified roles.
    /// Multiple instances on the same class are combined with implicit AND.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequireRolesAttribute : Attribute
    {
        /// <summary>Gets the role names required by this clause.</summary>
        public string[] Roles { get; }

        /// <summary>Gets or sets how the roles are matched. Default is <see cref="AuthorizationMatch.All"/>.</summary>
        public new AuthorizationMatch Match { get; set; } = AuthorizationMatch.All;

        /// <summary>Gets or sets the minimum number of matching roles when <see cref="Match"/> is <see cref="AuthorizationMatch.AtLeast"/>. Default is 0.</summary>
        public int Minimum { get; set; } = 0;

        /// <summary>
        /// Initializes a new instance of <see cref="RequireRolesAttribute"/> with the specified role names.
        /// </summary>
        /// <param name="roles">The role names required by this clause.</param>
        public RequireRolesAttribute(params string[] roles)
        {
            Roles = roles;
        }
    }
}
