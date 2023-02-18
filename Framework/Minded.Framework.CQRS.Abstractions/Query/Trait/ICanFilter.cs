using System;
using System.Linq.Expressions;

namespace Minded.Framework.CQRS.Query.Trait
{
    /// <summary>
    /// Use this interface only to create new ICanFilter traits using different types of filtering
    /// </summary>
    /// <typeparam name="T">Filtered type</typeparam>
    public interface ICanFilter<T>
    {

    }

    /// <summary>
    /// Trait which enable a query to expose Expression based filtering
    /// </summary>
    /// <typeparam name="T">Filtered type</typeparam>
    public interface ICanFilterExpression<T> : ICanFilter<T>
    {
        Expression<Func<T, bool>> Filter { get; set; }
    }
}
