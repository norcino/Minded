using System;

namespace Minded.Extensions.Authorization.Attributes
{
    /// <summary>
    /// Declares that the command or query requires a specific claim predicate.
    /// Multiple instances on the same class are combined with implicit AND.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequireClaimAttribute : Attribute
    {
        /// <summary>Gets the claim key to evaluate.</summary>
        public string ClaimType { get; }

        /// <summary>
        /// Gets the allowed claim values for static evaluation.
        /// Ignored when <see cref="MatchProperty"/> is specified.
        /// </summary>
        public string[] Values { get; }

        /// <summary>Gets or sets how claim values are matched. Default is <see cref="AuthorizationMatch.All"/>.</summary>
        public new AuthorizationMatch Match { get; set; } = AuthorizationMatch.All;

        /// <summary>Gets or sets the minimum number of matching values when <see cref="Match"/> is <see cref="AuthorizationMatch.AtLeast"/>.</summary>
        public int Minimum { get; set; } = 0;

        /// <summary>
        /// Gets or sets the request property name used for dynamic claim matching.
        /// When specified, request property value is compared to the caller claim value.
        /// </summary>
        public string MatchProperty { get; set; }

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
        /// Initializes a new instance of <see cref="RequireClaimAttribute"/>.
        /// </summary>
        /// <param name="claimType">The claim key to evaluate.</param>
        /// <param name="values">Allowed claim values for static matching.</param>
        public RequireClaimAttribute(string claimType, params string[] values)
        {
            ClaimType = claimType;
            Values = values;
        }
    }
}
