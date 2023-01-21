using System;
using System.Linq.Expressions;

namespace Minded.Framework.CQRS.Query.Trait
{
    public interface ICanFilter<T>
    {

    }

    public interface ICanFilterExpression<T> : ICanFilter<T>
    {
        Expression<Func<T, bool>> Filter { get; set; }
    }
}
