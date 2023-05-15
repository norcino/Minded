using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Minded.Extensions.Caching.Abstractions.Decorator;
using Minded.Extensions.Caching.Decorator;
using Minded.Extensions.Configuration;

namespace Minded.Extensions.Caching.Memory.Decorator
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the Caching decorator for Queries
        /// </summary>
        public static MindedBuilder AddQueryCacheDecorator(this MindedBuilder builder)
        {
            if(!builder.ServiceCollection.Any(serviceDescriptor => serviceDescriptor.ServiceType == typeof(IGlobalCacheKeyPrefixProvider)))
            {
                builder.ServiceCollection.AddScoped<IGlobalCacheKeyPrefixProvider, NullGlobalCacheKeyPrefixProvider>();
            }

            builder.QueueQueryDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(MemoryCacheQueryHandlerDecorator<,>)));
            return builder;
        }
    }
}
