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
        public Order Order { get; set; }
        public string PropertyName { get; set; }

        public new string ToString()
        {
            return $"{PropertyName} {Order}";
        }
    }
}
