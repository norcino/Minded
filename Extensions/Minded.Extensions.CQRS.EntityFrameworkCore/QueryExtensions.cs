using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query.Trait;

namespace Minded.Framework.CQRS.Query
{
    /// <summary>
    /// Extension methods that apply <see cref="IQuery{TResult}"/> trait-based configuration
    /// (filtering, ordering, pagination, eager loading) to an <see cref="IQueryable{T}"/> source
    /// via Entity Framework Core.
    /// </summary>
    public static class QueryExtensions
    {
        /// <summary>
        /// Applies the query trait configuration to an <see cref="IQueryable{T}"/> and returns the
        /// shaped queryable for use with <see cref="IQueryResponse{TResult}"/> wrapping an
        /// <see cref="IEnumerable{T}"/>.
        /// Respects <see cref="ICanOrderBy"/>, <see cref="ICanExpand"/>, <see cref="ICanFilterExpression{T}"/>,
        /// <see cref="ICanSkip"/>, <see cref="ICanTop"/> and <see cref="ICanCount"/> traits.
        /// Defaults to a maximum of 100 results when <see cref="ICanTop"/> is not set.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">Query carrying the trait configuration.</param>
        /// <param name="queryable">IQueryable source to shape.</param>
        /// <returns>Shaped <see cref="IQueryable{T}"/> ready for materialisation.</returns>
        public static IQueryable<T> ApplyQueryTo<T>(this IQuery<IQueryResponse<IEnumerable<T>>> query, IQueryable<T> queryable) where T : class
        {
            if (query is ICanOrderBy o && o?.OrderBy?.Count > 0)
            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                IOrderedQueryable<T> oq = null;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                foreach (OrderDescriptor order in o.OrderBy)
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
        /// Applies the query trait configuration to an <see cref="IQueryable{T}"/> and returns the
        /// shaped queryable for use with queries returning a plain <see cref="IEnumerable{T}"/>.
        /// Respects <see cref="ICanOrderBy"/>, <see cref="ICanExpand"/>, <see cref="ICanFilterExpression{T}"/>,
        /// <see cref="ICanSkip"/>, <see cref="ICanTop"/> and <see cref="ICanCount"/> traits.
        /// Defaults to a maximum of 100 results when <see cref="ICanTop"/> is not set.
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
                foreach(OrderDescriptor order in o.OrderBy)
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
        /// Applies filter and expand traits to the queryable and returns the first matching entity,
        /// or <c>null</c> when no match is found.
        /// Note: <see cref="ICanCount"/>, <see cref="ICanTop"/>, <see cref="ICanSkip"/> and <see cref="ICanOrderBy"/> traits
        /// are intentionally ignored because they are not meaningful for single-entity queries.
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

        /// <summary>
        /// Applies filter and expand traits to the queryable and returns the first matching entity
        /// wrapped in an <see cref="IQueryResponse{TResult}"/>, or an empty response when no match is found.
        /// Note: <see cref="ICanCount"/>, <see cref="ICanTop"/>, <see cref="ICanSkip"/> and <see cref="ICanOrderBy"/> traits
        /// are intentionally ignored because they are not meaningful for single-entity queries.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">Query carrying the trait configuration.</param>
        /// <param name="queryable">IQueryable source to query against.</param>
        /// <returns>An <see cref="IQueryResponse{TResult}"/> wrapping the first matched entity.</returns>
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

            T result = await queryable.FirstOrDefaultAsync();

            var response = (IQueryResponse<T>)Activator.CreateInstance(typeof(T), result);
            return (IQueryResponse<T>)response;

            //return ((T) await queryable.FirstOrDefaultAsync());
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        }

        #region Private methods
        /// <summary>
        /// Builds a lambda expression of the form <c>x => (object)x.PropertyName</c>
        /// used to pass a property selector to LINQ ordering methods.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="propertyName">Name of the property to project (case-sensitive).</param>
        /// <returns>A compiled lambda suitable for use with <see cref="Queryable.OrderBy{TSource,TKey}"/>.</returns>
        private static Expression<Func<T, object>> LambdaOf<T>(string propertyName)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(T));
            MemberExpression property = Expression.Property(parameter, propertyName);
            UnaryExpression propertyObject = Expression.Convert(property, typeof(object));

            return Expression.Lambda<Func<T, object>>(propertyObject, parameter);
        }
        #endregion
    }
}
