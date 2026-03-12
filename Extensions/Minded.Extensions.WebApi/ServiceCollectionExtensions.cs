using System;
using Microsoft.Extensions.DependencyInjection;
using Minded.Extensions.Configuration;

namespace Minded.Extensions.WebApi
{
    public static class ServiceCollectionExtensions
    {
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
