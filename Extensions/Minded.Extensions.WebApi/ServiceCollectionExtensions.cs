using Microsoft.Extensions.DependencyInjection;
using Minded.Extensions.Configuration;

namespace Minded.Extensions.WebApi
{
    public static class ServiceCollectionExtensions
    {
        public static void AddRestMediator(this MindedBuilder builder, ServiceLifetime lifeTime = ServiceLifetime.Transient)
        {
            builder.Register(sc => sc.Add(new ServiceDescriptor(typeof(IRestMediator), typeof(RestMediator), lifeTime)));
            builder.Register(sc => sc.Add(new ServiceDescriptor(typeof(IRestRulesProvider), typeof(RestRulesProvider), lifeTime)));
            builder.Register(sc => sc.Add(new ServiceDescriptor(typeof(IRulesProcessor), typeof(DefaultRulesProcessor), lifeTime)));
        }
    }
}
