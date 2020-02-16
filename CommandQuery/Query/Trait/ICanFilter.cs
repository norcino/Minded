using System;
using System.Linq.Expressions;

namespace Minded.Common.Query.Trait
{
    public interface ICanFilter<T>
    {
        Expression<Func<T, bool>> Filter { get; set; }
    }
}