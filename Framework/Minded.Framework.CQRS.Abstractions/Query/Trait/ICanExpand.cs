namespace Minded.Framework.CQRS.Query.Trait
{
    public interface ICanExpand
    {
        string[] Expand { get; set; }
    }
}
