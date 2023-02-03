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
    /// Describes the ordering type and which Property has been ordered
    /// </summary>
    public class OrderDescriptor
    {
        public OrderDescriptor(Order order, string propertyName)
        {
            Order = order;
            PropertyName = propertyName;
        }

        public Order Order { get; }
        public string PropertyName { get; }

        public new string ToString()
        {
            return $"{PropertyName} {Order}";
        }
    }
}
