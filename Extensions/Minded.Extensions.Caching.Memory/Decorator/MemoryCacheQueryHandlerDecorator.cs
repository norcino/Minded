using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Minded.Extensions.Caching.Abstractions.Decorator;
using Minded.Extensions.Caching.Decorator;
using Minded.Framework.CQRS.Query;
using Minded.Framework.Decorator;

namespace Minded.Extensions.Caching.Memory.Decorator
{
    internal static class Shared
    {
        /// <summary>
        /// Determine if the query requires caching
        /// </summary>
        /// <param name="query">Subject Query</param>
        /// <returns>True if the query requires validation</returns>
        internal static bool IsCachedQuery(object query)
        {
            bool implementsTheInterface = query is IGenerateCacheKey;
            return implementsTheInterface && TypeDescriptor.GetAttributes(query)[typeof(CacheAttribute)] != null;
        }
    }

    public class MemoryCacheQueryHandlerDecorator<TQuery, TResult> : QueryHandlerDecoratorBase<TQuery, TResult>, IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        private readonly IMemoryCache _cache;
        private readonly IGlobalCacheKeyPrefixProvider _globalCacheKeyPrefixProvider;

        public MemoryCacheQueryHandlerDecorator(IQueryHandler<TQuery, TResult> decoratedQueryHandler, IMemoryCache cache, IGlobalCacheKeyPrefixProvider globalCacheKeyPrefixProvider) : base(decoratedQueryHandler)
        {
            _cache = cache;
            _globalCacheKeyPrefixProvider = globalCacheKeyPrefixProvider;
        }

        public async Task<TResult> HandleAsync(TQuery query)
        {
            MemoryCacheAttribute cacheAttribute = null;
            TResult result;
            string cacheKey = "";
            bool failed = false;

            try
            {
                if (!Shared.IsCachedQuery(query))
                {
                    // If the attribute is not set, just run the query as usual
                    return await InnerQueryHandler.HandleAsync(query);
                }

                cacheAttribute = (MemoryCacheAttribute)Attribute.GetCustomAttribute(query.GetType(), typeof(CacheAttribute));

                // If the attribute is set, use the cache
                cacheKey = $"{_globalCacheKeyPrefixProvider.GetGlobalCacheKeyPrefix()}-{((IGenerateCacheKey)query).GetCacheKey()}";

                if (_cache.TryGetValue(cacheKey, out result))
                {
                    // If the result is in the cache, return it
                    return result;
                }
            }
            catch(Exception)
            {
                failed = true;
                if (cacheAttribute != null && cacheAttribute.FailOnError)
                {
                    throw;
                }
            }

            // If the result is not in the cache, fetch it and add it to the cache
            result = await InnerQueryHandler.HandleAsync(query);

            try
            {
                if (failed || cacheAttribute == null)
                    return result;

                var cacheEntryOptions = new MemoryCacheEntryOptions();

                if (cacheAttribute.AbsoluteExpiration != default)
                {
                    if(DateTimeOffset.TryParse(cacheAttribute.AbsoluteExpiration, out DateTimeOffset dateTimeOffset))
                    {
                        cacheEntryOptions.AbsoluteExpiration = dateTimeOffset;
                    }
                    else
                    {
                        throw new ArgumentException("The CacheAttribute.AbsoluteExpiration string format is not a valid DateTimeOffset, use ISO 8601.");
                    }
                }

                if (cacheAttribute.ExpirationInSeconds != default)
                    cacheEntryOptions.AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromSeconds(cacheAttribute.ExpirationInSeconds);

                if (cacheAttribute.SlidingExpiration != default)
                    cacheEntryOptions.SlidingExpiration =
                        TimeSpan.FromSeconds(cacheAttribute.SlidingExpiration);

                _cache.Set(cacheKey, result, cacheEntryOptions);
            }
            catch (Exception)
            {
                if (cacheAttribute.FailOnError)
                {
                    throw;
                }
            }

            return result;
        }
    }
}
