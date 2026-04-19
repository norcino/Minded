namespace Minded.Framework.CQRS.Query.Trait
{
    /// <summary>
    /// Enum representing supported ordering types
    /// </summary>
    public enum Order
    {
        Ascending,
        Descending
    }

    /// <summary>
    /// Describes the ordering direction and the property name to sort by.
    /// Used within <see cref="ICanOrderBy"/> to express multi-property sort criteria.
    /// </summary>
    public class OrderDescriptor
    {
        /// <summary>
        /// Initializes a new <see cref="OrderDescriptor"/>.
        /// </summary>
        /// <param name="order">Sort direction for this descriptor.</param>
        /// <param name="propertyName">Name of the property to sort by (case-sensitive).</param>
        public OrderDescriptor(Order order, string propertyName)
        {
            Order = order;
            PropertyName = propertyName;
        }

        /// <summary>Sort direction (ascending or descending).</summary>
        public Order Order { get; }

        /// <summary>Name of the property to sort by (case-sensitive).</summary>
        public string PropertyName { get; }

        /// <summary>
        /// Returns a human-readable string representation in the form "PropertyName Direction".
        /// Example: "CreatedAt Descending".
        /// </summary>
        public new string ToString()
        {
            return $"{PropertyName} {Order}";
        }
    }
}
