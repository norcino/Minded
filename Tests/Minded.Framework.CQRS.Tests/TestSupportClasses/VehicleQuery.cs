using Minded.Framework.CQRS.Query;
using Minded.Framework.CQRS.Query.Trait;

namespace Minded.Framework.CQRS.Tests.TestSupportClasses
{
    public class VehicleQuery : IQuery<Vehicle>, ICanOrderBy
    {
        public IList<OrderDescriptor> OrderBy { get; set; }
    }
}
