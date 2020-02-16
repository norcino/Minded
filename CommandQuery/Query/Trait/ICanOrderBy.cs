using System.Collections.Generic;

namespace Minded.CommandQuery.Query.Trait
{
    public interface ICanOrderBy
    {
        IList<OrderDescriptor> OrderBy { get; set; }
    }
}