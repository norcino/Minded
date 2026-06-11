using System;
using System.Linq.Expressions;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Query;
using Minded.Framework.CQRS.Query.Trait;

namespace Minded.Framework.CQRS.Tests.TestSupportClasses
{
    /// <summary>
    /// Single-entity query returning the entity directly.
    /// Used to test the Task&lt;T&gt; ApplyQueryTo overload.
    /// </summary>
    internal class SingleVehicleQuery : IQuery<Vehicle>, ICanFilterExpression<Vehicle>, ICanExpand
    {
        /// <summary>
        /// LINQ expression used to filter the results.
        /// </summary>
        public Expression<Func<Vehicle, bool>> Filter { get; set; }

        /// <summary>
        /// Array of navigation property names to eagerly load.
        /// </summary>
        public string[] Expand { get; set; }

        public Guid TraceId => Guid.Empty;
    }

    /// <summary>
    /// Single-entity query returning the entity wrapped in an IQueryResponse.
    /// Used to test the Task&lt;IQueryResponse&lt;T&gt;&gt; ApplyQueryTo overload.
    /// </summary>
    internal class SingleVehicleResponseQuery : IQuery<IQueryResponse<Vehicle>>, ICanFilterExpression<Vehicle>, ICanExpand
    {
        /// <summary>
        /// LINQ expression used to filter the results.
        /// </summary>
        public Expression<Func<Vehicle, bool>> Filter { get; set; }

        /// <summary>
        /// Array of navigation property names to eagerly load.
        /// </summary>
        public string[] Expand { get; set; }

        public Guid TraceId => Guid.Empty;
    }
}
