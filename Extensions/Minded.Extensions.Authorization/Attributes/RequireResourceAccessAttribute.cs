using System;

namespace Minded.Extensions.Authorization.Attributes
{
    /// <summary>
    /// Declares resource-level authorization for a command or query by dispatching a dedicated
    /// authorization query built from a request resource identifier and a caller claim value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequireResourceAccessAttribute : Attribute
    {
        /// <summary>Gets the request property name containing the target resource identifier.</summary>
        public string ResourceIdProperty { get; }

        /// <summary>Gets the claim key containing the caller identifier used in the authorization query.</summary>
        public string ResourceIdClaim { get; }

        /// <summary>Gets the query type to instantiate and dispatch for resource authorization.</summary>
        public Type QueryType { get; }

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
        /// Initializes a new instance of <see cref="RequireResourceAccessAttribute"/>.
        /// </summary>
        /// <param name="resourceIdProperty">Request property containing the resource identifier.</param>
        /// <param name="resourceIdClaim">Claim key containing caller identifier.</param>
        /// <param name="queryType">Authorization query type to dispatch.</param>
        public RequireResourceAccessAttribute(string resourceIdProperty, string resourceIdClaim, Type queryType)
        {
            ResourceIdProperty = resourceIdProperty;
            ResourceIdClaim = resourceIdClaim;
            QueryType = queryType;
        }
    }
}
