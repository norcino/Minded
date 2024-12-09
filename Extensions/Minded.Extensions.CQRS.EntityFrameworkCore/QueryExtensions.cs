using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query.Trait;

namespace Minded.Framework.CQRS.Query
{
    public static class QueryExtensions
    {
        public static IQueryable<T> ApplyQueryTo<T>(this IQuery<IQueryResponse<IEnumerable<T>>> query, IQueryable<T> queryable) where T : class
        {
            if (query is ICanOrderBy o && o?.OrderBy?.Count > 0)
            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                IOrderedQueryable<T> oq = null;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                foreach (var order in o.OrderBy)
                {
                    if (order.Order == Order.Ascending)
                    {
                        if (oq == null)
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

            if (query is ICanFilterExpression<T> f && f.Filter != null)
            {
                queryable = queryable.Where(f.Filter);
            }

            if (query is ICanSkip s && s.Skip.HasValue)
            {
                queryable = queryable.Skip(s.Skip.Value);
            }

            if (query is ICanTop t && t.Top.HasValue)
            {
                queryable = queryable.Take(t.Top.Value);
            }
            else
            {
                queryable = queryable.Take(100);
            }

            if (query is ICanCount c && c.Count)
            {
                c.CountValue = queryable.Count();
                if (c.CountOnly)
                    queryable.Take(0);
            }

            return queryable;
        }

        /// <summary>
        /// Applies the query to an IQueryable source and returns the matching entities or an empty list
        /// </summary>
        /// <typeparam name="T">Type of the elements in the resulting IEnumerable</typeparam>
        /// <param name="query">Query</param>
        /// <param name="queryable">IQueryable where the fetch the entity from</param>
        /// <returns>List of found entities</returns>
        public static IQueryable<T> ApplyQueryTo<T>(this IQuery<IEnumerable<T>> query, IQueryable<T> queryable) where T : class
        {
            if (query is ICanOrderBy o && o?.OrderBy?.Count > 0)
            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                IOrderedQueryable<T> oq = null;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
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

            if (query is ICanFilterExpression<T> f && f.Filter != null)
            {
                queryable = queryable.Where(f.Filter);
            }

            if (query is ICanSkip s && s.Skip.HasValue)
            {
                queryable = queryable.Skip(s.Skip.Value);
            }

            if (query is ICanTop t && t.Top.HasValue)
            {
                queryable = queryable.Take(t.Top.Value);
            }
            else
            {
                queryable = queryable.Take(100);
            }

            if (query is ICanCount c && c.Count)
            {
                c.CountValue = queryable.Count();
                if (c.CountOnly)
                    queryable.Take(0);
            }

            return queryable;
        }

        /// <summary>
        /// Applies the query to an IQueryable source and returns one entity if found, null otherwise.
        /// This method do not use any Trait which use would make sense for queries returning lists of objects.
        /// ICanCount, ICanTop, ICanSkip and ICanOrderBy are therefore ignored.
        /// </summary>
        /// <typeparam name="T">Returned type</typeparam>
        /// <param name="query">Query</param>
        /// <param name="queryable">IQueryable where the fetch the entity from</param>
        /// <returns>Found entity or null</returns>
        public async static Task<T> ApplyQueryTo<T>(this IQuery<T> query, IQueryable<T> queryable) where T : class
        {
            if (query is ICanExpand e && e.Expand?.Length > 0)
            {
                queryable = e.Expand.Aggregate(queryable, (current, expand) => current.Include(expand));
            }

            if (query is ICanFilterExpression<T> f && f.Filter != null)
            {
                queryable = queryable.Where(f.Filter);
            }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.

            //var result = await queryable.FirstOrDefaultAsync();

            //if (typeof(IQueryResponse<>).MakeGenericType(typeof(T)).IsAssignableFrom(typeof(T)))
            //{
            //    var response = (IQueryResponse<T>)Activator.CreateInstance(typeof(T), result);
            //    return (T)response;
            //}

            //return result;

            return ((T) await queryable.FirstOrDefaultAsync());
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        }

        public async static Task<IQueryResponse<T>> ApplyQueryTo<T>(this IQuery<IQueryResponse<T>> query, IQueryable<T> queryable) where T : class
        {
            if (query is ICanExpand e && e.Expand?.Length > 0)
            {
                queryable = e.Expand.Aggregate(queryable, (current, expand) => current.Include(expand));
            }

            if (query is ICanFilterExpression<T> f && f.Filter != null)
            {
                queryable = queryable.Where(f.Filter);
            }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.

            var result = await queryable.FirstOrDefaultAsync();

            var response = (IQueryResponse<T>)Activator.CreateInstance(typeof(T), result);
            return (IQueryResponse<T>)response;

            //return ((T) await queryable.FirstOrDefaultAsync());
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        }

        #region Private methods
        private static Expression<Func<T, object>> LambdaOf<T>(string propertyName)
        {
            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, propertyName);
            var propertyObject = Expression.Convert(property, typeof(object));

            return Expression.Lambda<Func<T, object>>(propertyObject, parameter);
        }
        #endregion
    }
}
