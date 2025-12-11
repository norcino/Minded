using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Minded.Framework.CQRS.Query;
using Minded.Framework.CQRS.Query.Trait;

namespace Minded.Framework.CQRS.Tests.TestSupportClasses
{
    /// <summary>
    /// A comprehensive query class that implements all available query traits.
    /// Used for testing the full functionality of QueryExtensions.ApplyQueryTo method.
    /// </summary>
    /// <remarks>
    /// This query implements:
    /// - ICanOrderBy: Enables ordering by property name (ascending/descending)
    /// - ICanExpand: Enables eager loading of related entities via Include()
    /// - ICanFilterExpression: Enables LINQ expression-based filtering via Where()
    /// - ICanSkip: Enables skipping records for pagination
    /// - ICanTop: Enables limiting the number of records returned
    /// - ICanCount: Enables counting of matching records
    /// </remarks>
    internal class FullFeaturedVehicleQuery : IQuery<IEnumerable<Vehicle>>,
        ICanOrderBy,
        ICanExpand,
        ICanFilterExpression<Vehicle>,
        ICanSkip,
        ICanTop,
        ICanCount
    {
        #region ICanOrderBy
        /// <summary>
        /// List of order descriptors specifying the ordering of results.
        /// Each descriptor contains the property name and direction (Ascending/Descending).
        /// </summary>
        public IList<OrderDescriptor> OrderBy { get; set; }
        #endregion

        #region ICanExpand
        /// <summary>
        /// Array of navigation property names to eagerly load.
        /// These are passed to EF Core's Include() method.
        /// Example: new[] { "Owner", "Maker" }
        /// </summary>
        public string[] Expand { get; set; }
        #endregion

        #region ICanFilterExpression
        /// <summary>
        /// LINQ expression used to filter the results.
        /// Applied as a Where() clause on the queryable.
        /// Example: v => v.Model.Contains("BMW")
        /// </summary>
        public Expression<Func<Vehicle, bool>> Filter { get; set; }
        #endregion

        #region ICanSkip
        /// <summary>
        /// Number of records to skip. Used for pagination.
        /// Applied as Skip() on the queryable.
        /// </summary>
        public int? Skip { get; set; }
        #endregion

        #region ICanTop
        /// <summary>
        /// Maximum number of records to return.
        /// Applied as Take() on the queryable.
        /// If null, defaults to 100 to prevent unbounded queries.
        /// </summary>
        public int? Top { get; set; }
        #endregion

        #region ICanCount
        /// <summary>
        /// When true, only returns the count without any data.
        /// </summary>
        public bool CountOnly { get; set; }

        /// <summary>
        /// When true, the CountValue property will be populated with the total count.
        /// </summary>
        public bool Count { get; set; }

        /// <summary>
        /// Contains the count of matching records after query execution.
        /// Only populated when Count is true.
        /// </summary>
        public int CountValue { get; set; }
        #endregion

        #region IQuery
        /// <summary>
        /// Unique identifier for tracing this query across the system.
        /// </summary>
        public Guid TraceId { get; } = Guid.NewGuid();
        #endregion
    }
}

