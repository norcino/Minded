using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query.Trait;

namespace Minded.Framework.CQRS.Query
{
    public static class QueryExtensions
    {
        private static Expression<Func<T,object>> LambdaOf<T>(string propertyName)
        {
            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, propertyName);
            var propertyObject = Expression.Convert(property, typeof(object));

            return Expression.Lambda<Func<T, object>>(propertyObject, parameter);
        }

        public static IQueryable<T> ApplyTo<T>(this IQuery<T> query, IQueryable<T> queryable) where T : class
        {
            if (query is ICanOrderBy o && o?.OrderBy?.Count > 0)
            {
                IOrderedQueryable<T> oq = null;
                foreach(var order in o.OrderBy)
                {
                    if(order.Order == Order.Ascending)
                    {
                        if(oq == null)
                            oq = queryable.OrderBy(LambdaOf<T>(order.PropertyName));
                        else
                            oq = oq.ThenBy(LambdaOf<T>(order.PropertyName));
                    }
                    else
                    {
                        if (oq == null)
                            oq = queryable.OrderByDescending(LambdaOf<T>(order.PropertyName));
                        else
                            oq = oq.ThenByDescending(LambdaOf<T>(order.PropertyName));
                    }
                }

                queryable = oq ?? queryable;
            }

            if (query is ICanExpand e && e.Expand?.Length > 0)
            {
               queryable = e.Expand.Aggregate(queryable, (current, expand) => current.Include(expand));
            }

            //if (query is ICanFilter<T> f && f.Filter != null)
            //{
            //    queryable = queryable.Where(f.Filter);
            //}

            if (query is ICanSkip s)
            {
                queryable = queryable.Skip(s.Skip);
            }

            if (query is ICanTop t && t.Top.HasValue)
            {
                queryable = queryable.Take(t.Top.Value);
            }

            return queryable;
        }
    }
}
