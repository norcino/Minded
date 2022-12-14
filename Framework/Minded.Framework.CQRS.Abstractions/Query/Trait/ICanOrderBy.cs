using System.Collections.Generic;

namespace Minded.Framework.CQRS.Query.Trait
{
    public interface ICanOrderBy
    {
        IList<OrderDescriptor> OrderBy { get; set; }
    }
}
