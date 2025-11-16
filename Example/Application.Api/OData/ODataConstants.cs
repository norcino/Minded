namespace Application.Api.OData
{
    /// <summary>
    /// Constants used for OData navigation property serialization.
    /// </summary>
    public static class ODataConstants
    {
        /// <summary>
        /// HttpContext.Items key for storing the list of properties explicitly requested via OData $expand parameter.
        /// </summary>
        /// <remarks>
        /// This key is used by <see cref="ODataExpandActionFilter"/> to store the set of expanded property names,
        /// and by <see cref="IgnoreNavigationPropertiesResolver"/> to determine which navigation properties 
        /// should be included in JSON serialization.
        /// 
        /// The value stored is a <see cref="System.Collections.Generic.HashSet{T}"/> of <see cref="string"/> 
        /// containing the property names that were explicitly requested via the $expand query parameter.
        /// </remarks>
        public const string ExpandedPropertiesKey = "ODataExpandedProperties";
    }
}

