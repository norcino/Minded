using Microsoft.Extensions.DependencyInjection;
using Minded.Extensions.Configuration;

namespace Minded.Framework.Mediator
{
    /// <summary>
    /// Registration extensions for the Mediator pattern.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Sets the necessary dependency injection configuration to use the Mediator pattern,
        /// registering <see cref="IMediator"/> to resolve to <see cref="Mediator"/>.
        /// </summary>
        /// <param name="builder">The <see cref="MindedBuilder"/> instance used to register the mediator.</param>
        /// <param name="lifeTime">DI lifetime for the registered mediator. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
        public static void AddMediator(this MindedBuilder builder, ServiceLifetime lifeTime = ServiceLifetime.Transient)
        {
            var serviceDescriptor = new ServiceDescriptor(typeof(IMediator), typeof(Mediator), lifeTime);
            builder.Register(sc => sc.Add(serviceDescriptor));
        }
    }
}
