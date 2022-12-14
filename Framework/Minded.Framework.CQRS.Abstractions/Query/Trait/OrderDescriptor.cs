namespace Minded.Framework.CQRS.Query.Trait
{
    public enum Order
    {
        Ascending,
        Descending
    }

    public class OrderDescriptor
    {
        public Order Order { get; set; }
        public string PropertyName { get; set; }
    }
}
