using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNet.OData.Query;

namespace Minded.Extensions.OData
{
    public static class ODataQueryOptionExtensions
    {
        /// <summary>
        /// Extract the expression used to filter teh queryable. This expression can be applied to the Context.
        /// </summary>
        /// <typeparam name="TEntity">Entity type which will be filtered</typeparam>
        /// <param name="filter">FilterQueryOption filter from the ODataQueryOption</param>
        /// <returns>Expression which can be uset to filter an IQueryable of TEntity</returns>
        public static Expression<Func<TEntity, bool>> GetFilterExpression<TEntity>(this FilterQueryOption filter)
        {
            var enumerable = Enumerable.Empty<TEntity>().AsQueryable();
            var param = Expression.Parameter(typeof(TEntity));
            if (filter != null)
            {
                enumerable = (IQueryable<TEntity>)filter.ApplyTo(enumerable, new ODataQuerySettings());

                var mce = enumerable.Expression as MethodCallExpression;
                if (mce != null)
                {
                    var quote = mce.Arguments[1] as UnaryExpression;
                    if (quote != null)
                    {
                        return quote.Operand as Expression<Func<TEntity, bool>>;
                    }
                }
            }
            return Expression.Lambda<Func<TEntity, bool>>(Expression.Constant(true), param);
        }
    }
}
