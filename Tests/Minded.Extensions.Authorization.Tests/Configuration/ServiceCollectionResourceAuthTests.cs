using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minded.Extensions.Authorization.Configuration;
using Minded.Extensions.Configuration;
using Minded.Extensions.Context;

namespace Minded.Extensions.Authorization.Tests.Configuration;

[TestClass]
public class ServiceCollectionResourceAuthTests
{
    private static MindedBuilder CreateBuilder(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder().Build();
        return new MindedBuilder(services, configuration, _ => false);
    }

    [TestMethod]
    public void AddCommandAuthorizationDecorator_RegistersFallbackMindedContextAccessor()
    {
        var services = new ServiceCollection();
        var builder = CreateBuilder(services);

        builder.AddCommandAuthorizationDecorator();

        services.Any(d => d.ServiceType == typeof(IMindedContextAccessor)).Should().BeTrue();
    }

    [TestMethod]
    public void AddQueryAuthorizationDecorator_RegistersFallbackMindedContextAccessor()
    {
        var services = new ServiceCollection();
        var builder = CreateBuilder(services);

        builder.AddQueryAuthorizationDecorator();

        services.Any(d => d.ServiceType == typeof(IMindedContextAccessor)).Should().BeTrue();
    }

    [TestMethod]
    public void AddCommandAuthorizationDecorator_DoesNotOverrideExistingMindedContextAccessor()
    {
        var services = new ServiceCollection();
        var existing = new MindedContextAccessor();
        services.AddSingleton<IMindedContextAccessor>(existing);

        var builder = CreateBuilder(services);
        builder.AddCommandAuthorizationDecorator();

        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IMindedContextAccessor>().Should().BeSameAs(existing);
    }
}
