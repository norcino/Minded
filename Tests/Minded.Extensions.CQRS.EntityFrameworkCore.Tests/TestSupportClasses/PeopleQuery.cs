using System;
using System.Collections.Generic;
using Minded.Framework.CQRS.Query;
using Minded.Framework.CQRS.Query.Trait;

namespace Minded.Framework.CQRS.Tests.TestSupportClasses
{
    public class PeopleQuery : IQuery<IEnumerable<Person>>, ICanOrderBy, ICanExpand
    {
        public IList<OrderDescriptor> OrderBy { get; set; }
        public string[] Expand { get; set; }

        public Guid TraceId => throw new NotImplementedException();
    }
}
