using System;
using System.Collections.Generic;
using Minded.Framework.CQRS.Query;
using Minded.Framework.CQRS.Query.Trait;

namespace Minded.Framework.CQRS.Tests.TestSupportClasses
{
    public class VehicleQuery : IQuery<IEnumerable<Vehicle>>, ICanOrderBy
    {
        public IList<OrderDescriptor> OrderBy { get; set; }

        public Guid TraceId => throw new NotImplementedException();
    }
}
