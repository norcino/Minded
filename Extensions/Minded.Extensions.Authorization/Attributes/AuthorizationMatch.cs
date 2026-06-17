namespace Minded.Extensions.Authorization.Attributes
{
    /// <summary>
    /// Specifies how roles or permissions within a single clause are matched.
    /// </summary>
    public enum AuthorizationMatch
    {
        /// <summary>Every item in the clause must be present.</summary>
        All,

        /// <summary>At least one item in the clause must be present.</summary>
        Any,

        /// <summary>At least <see cref="RequireRolesAttribute.Minimum"/> items from the clause must be present.</summary>
        AtLeast,

        /// <summary>None of the items in the clause may be present.</summary>
        None
    }
}
