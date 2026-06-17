using System.Collections.Generic;

namespace Minded.Framework.CQRS.Query.Trait
{
    /// <summary>
    /// Trait that enables a query to specify one or more ordering criteria.
    /// Implement this interface on a query class to allow callers to control the sort order of results.
    /// </summary>
    public interface ICanOrderBy
    {
        /// <summary>
        /// Ordered list of <see cref="OrderDescriptor"/> values that define the sort order of the results.
        /// Descriptors are applied in list order (primary sort first, then secondary, etc.).
        /// </summary>
        IList<OrderDescriptor> OrderBy { get; set; }
    }
}
