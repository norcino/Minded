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
        /// Add the Memory Cache decorator for Queries and configures the EmptyGlobackCacheKeyPrefixProvider as default IGlobalCacheKeyPrefixProvider
        /// </summary>
        public static MindedBuilder AddQueryMemoryCacheDecorator(this MindedBuilder builder)
        {
            if(!builder.ServiceCollection.Any(serviceDescriptor => serviceDescriptor.ServiceType == typeof(IGlobalCacheKeyPrefixProvider)))
            {
                builder.ServiceCollection.AddScoped<IGlobalCacheKeyPrefixProvider, EmptyGlobalCacheKeyPrefixProvider>();
            }

            builder.QueueQueryDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(MemoryCacheQueryHandlerDecorator<,>)));
            return builder;
        }
    }
}
