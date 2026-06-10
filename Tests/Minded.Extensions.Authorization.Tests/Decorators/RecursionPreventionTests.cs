using System;
using System.Threading.Tasks;
using FluentAssertions;
using Minded.Extensions.Context;

namespace Minded.Extensions.Authorization.Tests.Decorators;

[TestClass]
public class RecursionPreventionTests
{
    private readonly struct AuthorizationBypass { }

    [TestMethod]
    public async Task BeginScope_FlowsBypassMarkerThroughAsyncCalls()
    {
        var context = new MindedContext(Guid.NewGuid(), DateTimeOffset.UtcNow, default, null);

        using (context.BeginScope(new AuthorizationBypass()))
        {
            await Task.Yield();
            context.TryGetScoped<AuthorizationBypass>(out _).Should().BeTrue();
        }

        context.TryGetScoped<AuthorizationBypass>(out _).Should().BeFalse();
    }

    [TestMethod]
    public async Task BeginScope_NestedScopesPreserveOuterValueOnDispose()
    {
        var context = new MindedContext(Guid.NewGuid(), DateTimeOffset.UtcNow, default, null);

        using (context.BeginScope(new AuthorizationBypass()))
        {
            using (context.BeginScope(new AuthorizationBypass()))
            {
                await Task.Yield();
                context.TryGetScoped<AuthorizationBypass>(out _).Should().BeTrue();
            }

            context.TryGetScoped<AuthorizationBypass>(out _).Should().BeTrue();
        }

        context.TryGetScoped<AuthorizationBypass>(out _).Should().BeFalse();
    }

    [TestMethod]
    public async Task ParallelTasks_DoNotLeakBypassMarkerAcrossBranches()
    {
        var context = new MindedContext(Guid.NewGuid(), DateTimeOffset.UtcNow, default, null);

        var taskWithScope = Task.Run(async () =>
        {
            using (context.BeginScope(new AuthorizationBypass()))
            {
                await Task.Delay(20);
                return context.TryGetScoped<AuthorizationBypass>(out _);
            }
        });

        var taskWithoutScope = Task.Run(async () =>
        {
            await Task.Delay(10);
            return context.TryGetScoped<AuthorizationBypass>(out _);
        });

        var results = await Task.WhenAll(taskWithScope, taskWithoutScope);
        results[0].Should().BeTrue();
        results[1].Should().BeFalse();
    }
}
