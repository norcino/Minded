using System;
using Microsoft.Extensions.DependencyInjection;
using Minded.Extensions.Configuration;

namespace Minded.Extensions.WebApi
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the REST mediator infrastructure: <see cref="IRestMediator"/>, <see cref="IRestRulesProvider"/>
        /// and <see cref="IRulesProcessor"/>.
        /// </summary>
        /// <param name="builder">The <see cref="MindedBuilder"/> instance.</param>
        /// <param name="lifeTime">DI lifetime for the registered services. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
        /// <param name="iRestRulesProviderType">
        /// Optional custom <see cref="IRestRulesProvider"/> implementation type.
        /// When <c>null</c> <see cref="DefaultRestRulesProvider"/> is used.
        /// </param>
        /// <param name="iDefaultRulesProcessor">
        /// Optional custom <see cref="IRulesProcessor"/> implementation type.
        /// When <c>null</c> <see cref="DefaultRulesProcessor"/> is used.
        /// </param>
        public static void AddRestMediator(this MindedBuilder builder, ServiceLifetime lifeTime = ServiceLifetime.Transient,
            Type iRestRulesProviderType = null,
            Type iDefaultRulesProcessor = null)
        {
            builder.Register(sc => sc.Add(new ServiceDescriptor(typeof(IRestMediator), typeof(RestMediator), lifeTime)));
            builder.Register(sc => sc.Add(new ServiceDescriptor(typeof(IRestRulesProvider), iRestRulesProviderType ?? typeof(DefaultRestRulesProvider), lifeTime)));
            builder.Register(sc => sc.Add(new ServiceDescriptor(typeof(IRulesProcessor), iDefaultRulesProcessor ?? typeof(DefaultRulesProcessor), lifeTime)));
        }
    }
}
