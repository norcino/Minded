using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minded.Extensions.Authorization;
using Minded.Extensions.Authorization.Configuration;
using Minded.Extensions.Configuration;
using Microsoft.Extensions.Configuration;

namespace Minded.Extensions.Authorization.Tests.Configuration;

#region Test Types for Registration

public class TestAuthorizationContextAccessor : IAuthorizationContextAccessor
{
    public AuthorizationContext Current => new AuthorizationContext(true);
}

public class CustomEvaluator : IRequestAuthorizationEvaluator
{
    public AuthorizationDecision Evaluate(Type requestType, AuthorizationDescriptor descriptor, AuthorizationContext context)
        => AuthorizationDecision.Allow();
}

#endregion

[TestClass]
public class ServiceCollectionExtensionsTests
{
    private static MindedBuilder CreateBuilder(IServiceCollection services = null)
    {
        services ??= new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        // Use an assembly filter that matches nothing to avoid scanning real assemblies
        // during unit tests (we only want to test registration, not attribute validation)
        return new MindedBuilder(services, configuration, a => false);
    }

    /// <summary>
    /// Verify AddCommandAuthorizationDecorator registers decorators and returns builder.
    /// **Validates: Requirements 17.1, 17.3**
    /// </summary>
    [TestMethod]
    public void AddCommandAuthorizationDecorator_RegistersDecoratorsAndReturnsBuilder()
    {
        var services = new ServiceCollection();
        var builder = CreateBuilder(services);

        var result = builder.AddCommandAuthorizationDecorator();

        result.Should().BeSameAs(builder);
        // Should have queued both command and command-with-result decorator actions
        builder.QueuedCommandDecoratorsRegistrationAction.Should().HaveCountGreaterThanOrEqualTo(1);
        builder.QueuedCommandWithResultDecoratorsRegistrationAction.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    /// <summary>
    /// Verify AddQueryAuthorizationDecorator registers decorators and returns builder.
    /// **Validates: Requirements 17.2, 17.3**
    /// </summary>
    [TestMethod]
    public void AddQueryAuthorizationDecorator_RegistersDecoratorsAndReturnsBuilder()
    {
        var services = new ServiceCollection();
        var builder = CreateBuilder(services);

        var result = builder.AddQueryAuthorizationDecorator();

        result.Should().BeSameAs(builder);
        builder.QueuedQueryDecoratorsRegistrationAction.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    /// <summary>
    /// Verify AddAuthorizationContextAccessor registers scoped accessor.
    /// **Validates: Requirements 17.4**
    /// </summary>
    [TestMethod]
    public void AddAuthorizationContextAccessor_RegistersScopedAccessor()
    {
        var services = new ServiceCollection();

        var result = services.AddAuthorizationContextAccessor<TestAuthorizationContextAccessor>();

        result.Should().BeSameAs(services);
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAuthorizationContextAccessor));
        descriptor.Should().NotBeNull();
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
        descriptor.ImplementationType.Should().Be(typeof(TestAuthorizationContextAccessor));
    }

    /// <summary>
    /// Verify AddRequestAuthorizationEvaluator registers singleton evaluator.
    /// **Validates: Requirements 17.5**
    /// </summary>
    [TestMethod]
    public void AddRequestAuthorizationEvaluator_RegistersSingletonEvaluator()
    {
        var services = new ServiceCollection();

        var result = services.AddRequestAuthorizationEvaluator<CustomEvaluator>();

        result.Should().BeSameAs(services);
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IRequestAuthorizationEvaluator));
        descriptor.Should().NotBeNull();
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
        descriptor.ImplementationType.Should().Be(typeof(CustomEvaluator));
    }

    /// <summary>
    /// Verify default evaluator is registered when no custom evaluator provided.
    /// **Validates: Requirements 17.1, 17.2**
    /// </summary>
    [TestMethod]
    public void AddCommandAuthorizationDecorator_RegistersDefaultEvaluator_WhenNoCustomProvided()
    {
        var services = new ServiceCollection();
        var builder = CreateBuilder(services);

        builder.AddCommandAuthorizationDecorator();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IRequestAuthorizationEvaluator));
        descriptor.Should().NotBeNull();
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
        descriptor.ImplementationType.Should().Be(typeof(DefaultRequestAuthorizationEvaluator));
    }

    /// <summary>
    /// Verify default evaluator is registered for query decorator when no custom evaluator provided.
    /// **Validates: Requirements 17.2**
    /// </summary>
    [TestMethod]
    public void AddQueryAuthorizationDecorator_RegistersDefaultEvaluator_WhenNoCustomProvided()
    {
        var services = new ServiceCollection();
        var builder = CreateBuilder(services);

        builder.AddQueryAuthorizationDecorator();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IRequestAuthorizationEvaluator));
        descriptor.Should().NotBeNull();
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
        descriptor.ImplementationType.Should().Be(typeof(DefaultRequestAuthorizationEvaluator));
    }

    /// <summary>
    /// Verify options are configured when configure action is provided.
    /// **Validates: Requirements 17.7, 17.8**
    /// </summary>
    [TestMethod]
    public void AddCommandAuthorizationDecorator_ConfiguresOptions_WhenConfigureProvided()
    {
        var services = new ServiceCollection();
        var builder = CreateBuilder(services);

        builder.AddCommandAuthorizationDecorator(opts =>
        {
            opts.RequireAuthenticationForAllCommands = true;
        });

        // Verify that an IConfigureOptions<AuthorizationOptions> was registered
        var configureDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<AuthorizationOptions>));
        configureDescriptor.Should().NotBeNull();
    }

    /// <summary>
    /// Verify query options are configured when configure action is provided.
    /// **Validates: Requirements 17.7, 17.8**
    /// </summary>
    [TestMethod]
    public void AddQueryAuthorizationDecorator_ConfiguresOptions_WhenConfigureProvided()
    {
        var services = new ServiceCollection();
        var builder = CreateBuilder(services);

        builder.AddQueryAuthorizationDecorator(opts =>
        {
            opts.RequireAuthenticationForAllQueries = true;
        });

        // Verify that an IConfigureOptions<AuthorizationOptions> was registered
        var configureDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<AuthorizationOptions>));
        configureDescriptor.Should().NotBeNull();
    }

    /// <summary>
    /// Verify fluent chaining works for both command and query decorators.
    /// **Validates: Requirements 17.3**
    /// </summary>
    [TestMethod]
    public void FluentChaining_WorksForBothDecorators()
    {
        var services = new ServiceCollection();
        var builder = CreateBuilder(services);

        var result = builder
            .AddCommandAuthorizationDecorator()
            .AddQueryAuthorizationDecorator();

        result.Should().BeSameAs(builder);
    }
}
