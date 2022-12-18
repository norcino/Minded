using Microsoft.Extensions.DependencyInjection;
using Minded.Extensions.Configuration;

namespace Minded.Framework.Mediator
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Sets the necessary dependency injection configuration to use the Mediator pattern
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="lifeTime"></param>
        public static void AddMediator(this MindedBuilder builder, ServiceLifetime lifeTime = ServiceLifetime.Transient)
        {
            var serviceDescriptor = new ServiceDescriptor(typeof(IMediator), typeof(Mediator), lifeTime);
            builder.Register(sc => sc.Add(serviceDescriptor));
        }
    }
}
