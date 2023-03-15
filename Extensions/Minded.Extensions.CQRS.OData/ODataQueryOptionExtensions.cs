using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNet.OData.Query;
using Microsoft.OData.Edm;
using Minded.Framework.CQRS.Query;
using Minded.Framework.CQRS.Query.Trait;

namespace Minded.Extensions.CQRS.OData
{
    public static class ODataQueryOptionExtensions
    {
        /// <summary>
        /// Applies the ODataQueryOptions configuration to the IQuery, depending on the traits used on the Query
        /// </summary>
        /// <typeparam name="T">Type returned by the query</typeparam>
        /// <param name="query">IQuery to setup with the OData query options</param>
        /// <param name="options">OData query options to use for the IQuery setup</param>
        public static void ApplyODataQueryOptions<T>(this IQuery<T> query, ODataQueryOptions options)
        {
            var filter = options.Filter;
            var orderBy = options.OrderBy;
            var skip = options.Skip;
            var top = options.Top;
            var selectExpand = options.SelectExpand;
            var count = options.Count;

            if (count != null && query is ICanCount)
            {
                (query as ICanCount).Count = count.Value;
            }

            if (filter != null && query.GetType().GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition()).Contains(typeof(ICanFilterExpression<>)))
            {
                try
                {
                    if (!typeof(T).IsGenericType)
                    {
                        (query as ICanFilterExpression<T>).Filter = filter.GetFilterExpression<T>();
                    }
                    else
                    {
                        var genericTypeArgs = typeof(T).GenericTypeArguments;

                        if (genericTypeArgs != null && genericTypeArgs.Count() == 1)
                        {
                            var method = typeof(ODataQueryOptionExtensions).GetMethod("GetFilterExpression", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).MakeGenericMethod(genericTypeArgs[0]);
                            query.GetType().GetProperty("Filter").SetValue(query, method.Invoke(null, new[] { filter }));
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Unable to extract Odata Filter, the filter might not be supported or incorrect syntax", e);
                }
            }

            if (top != null && query is ICanTop)
            {
                (query as ICanTop).Top = top.Value;
            }

            if (skip != null && query is ICanSkip)
            {
                (query as ICanSkip).Skip = skip.Value;
            }

            if (selectExpand?.RawExpand != null && query is ICanExpand)
            {
                (query as ICanExpand).Expand = new[] { selectExpand.RawExpand.Replace("/",".") };
            }

            if (orderBy != null && query is ICanOrderBy)
            {
                var orderDescriptors = new List<OrderDescriptor>();
                foreach(var orderByNode in orderBy.OrderByNodes)
                {                    
                    orderDescriptors.Add(
                        new OrderDescriptor(
                            orderByNode.Direction.ToString() == Order.Ascending.ToString() ? Order.Ascending : Order.Descending,
                            ((EdmNamedElement)((OrderByPropertyNode) orderByNode).Property).Name));
                }
                (query as ICanOrderBy).OrderBy = orderDescriptors;
            }
        }

        /// <summary>
        /// Applies the ODataQueryOptions to the IQueryable depending on the traits used on the Query
        /// </summary>
        /// <typeparam name="T">Type returned by the query</typeparam>
        /// <param name="query">IQuery to setup with the OData query options</param>
        /// <param name="options">OData query options to use for the IQuery setup</param>
        /// <returns></returns>
        public static IEnumerable<T> ApplyODataQueryOptions<T>(this IQueryable<T> query, ODataQueryOptions options) where T : class, new()
        {
            if (options == null) return query;

            var queryable = options.ApplyTo(query);

            if (queryable is IQueryable<T> queryableEntity)
            {
                queryableEntity = queryableEntity.Take(100);
                return queryableEntity.AsEnumerable();
            }

            return UnwrapAll<T>(queryable).ToList();
        }

        #region Private methods
        /// <summary>
        /// Extract the expression used to filter the queryable. This expression can be applied to the Context.
        /// </summary>
        /// <typeparam name="TEntity">Entity type which will be filtered</typeparam>
        /// <param name="filter">FilterQueryOption filter from the ODataQueryOption</param>
        /// <returns>Expression which can be uset to filter an IQueryable of TEntity</returns>
        private static Expression<Func<TEntity, bool>> GetFilterExpression<TEntity>(this FilterQueryOption filter)
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

        private static IEnumerable<T> UnwrapAll<T>(this IQueryable queryable) where T : class, new()
        {
            foreach (var item in queryable)
            {
                yield return Unwrap<T>(item);
            }
        }

        private static T Unwrap<T>(object item) where T : class, new()
        {
            var instanceProperty = item.GetType().GetProperty("Instance");
            var value = (T)instanceProperty.GetValue(item);

            if (value != null) return value;

            value = new T();
            var containerProperty = item.GetType().GetProperty("Container");
            var container = containerProperty.GetValue(item);

            if (container == null) return (T)null;

            var containerType = container.GetType();
            var containerItem = container;
            var returnNull = true;

            for (var i = 0; containerItem != null; i++)
            {
                var containerItemType = containerItem.GetType();
                var containerItemValue = containerItemType.GetProperty("Value").GetValue(containerItem);

                if (containerItemValue == null)
                {
                    containerItem = containerType.GetProperty($"Next{i}")?.GetValue(container);
                    continue;
                }

                var containerItemName = containerItemType.GetProperty("Name").GetValue(containerItem) as string;
                var expandedProp = typeof(T).GetProperty(containerItemName);

                if (expandedProp.SetMethod == null)
                {
                    containerItem = containerType.GetProperty($"Next{i}")?.GetValue(container);
                    continue;
                }

                if (containerItemValue.GetType() != typeof(string) && containerItemValue is IEnumerable containerValues)
                {
                    var listType = typeof(List<>).MakeGenericType(expandedProp.PropertyType.GenericTypeArguments[0]);
                    var expandedList = (IList)Activator.CreateInstance(listType);

                    foreach (var expandedItem in containerValues)
                    {
                        var expandedInstanceProp = expandedItem.GetType().GetProperty("Instance");
                        var expandedValue = expandedInstanceProp.GetValue(expandedItem);
                        expandedList.Add(expandedValue);
                    }

                    expandedProp.SetValue(value, expandedList);
                    returnNull = false;
                }
                else
                {
                    var expandedInstanceProp = containerItemValue.GetType().GetProperty("Instance");

                    if (expandedInstanceProp == null)
                    {
                        expandedProp.SetValue(value, containerItemValue);
                        returnNull = false;
                    }
                    else
                    {
                        var expandedValue = expandedInstanceProp.GetValue(containerItemValue);
                        if (expandedValue != null)
                        {
                            expandedProp.SetValue(value, expandedValue);
                            returnNull = false;
                        }
                        else
                        {
                            var genericType = containerItemValue.GetType().GenericTypeArguments[0];
                            var unwrapMethod = typeof(ODataQueryOptionExtensions).GetMethod(nameof(Unwrap));
                            var unwrapGenericMethod = unwrapMethod.MakeGenericMethod(genericType);
                            expandedValue = unwrapGenericMethod.Invoke(null, new[] { containerItemValue });
                            if (expandedValue != null)
                            {
                                expandedProp.SetValue(value, expandedValue);
                                returnNull = false;
                            }
                        }
                    }
                }
                containerItem = containerType.GetProperty($"Next{i}")?.GetValue(container);
            }

            if (returnNull) return (T)null;

            return value;
        }
        #endregion
    }
}
