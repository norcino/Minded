using Microsoft.Extensions.DependencyInjection;
using Minded.Extensions.Configuration;

namespace Minded.Framework.Mediator
{
    public static class ServiceCollectionExtensions
    {
        public static void AddMediator(this MindedBuilder builder, ServiceLifetime lifeTime = ServiceLifetime.Transient)
        {
            var serviceDescriptor = new ServiceDescriptor(typeof(IMediator), typeof(Mediator), lifeTime);
            builder.Register(sc => sc.Add(serviceDescriptor));
        }
    }
}
