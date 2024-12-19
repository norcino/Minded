using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Minded.Extensions.Caching.Abstractions.Decorator;
using Minded.Extensions.Caching.Decorator;
using Minded.Extensions.Configuration;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Query;
using Minded.Framework.Decorator;

namespace Minded.Extensions.Caching.Memory.Decorator
{
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
                // If the query doesn't have the MemoryCacheAttribute, just run the query as usual
                if (!(TypeDescriptor.GetAttributes(query)[typeof(MemoryCacheAttribute)] != null))
                {
                    // If the attribute is not set, just run the query as usual
                    return await InnerQueryHandler.HandleAsync(query);
                }

                // If the query doesn't implement IGenerateCacheKey
                if (!(query is IGenerateCacheKey))
                    throw new InvalidOperationException("The query must implement IGenerateCacheKey to be used with the MemoryCacheQueryHandlerDecorator.");

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
                // If the result is null or fail on error is disabled and was it gailed, skip the cache set
                if (failed || cacheAttribute == null || result == null)
                    return result;

                // If the query response type is IQueryResponse and it is not successful, do not cache the result
                if (TypeHelper.IsInterfaceOrImplementation(typeof(IQueryResponse<>), typeof(TResult)) && !(result as IMessageResponse).Successful)
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
