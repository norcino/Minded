using System;
using System.Threading;
using System.Threading.Tasks;
using AnonymousData;
using FluentAssertions;

namespace Minded.Extensions.Context.Tests
{
    /// <summary>
    /// Unit tests for the <see cref="MindedContext.BeginScope{T}(T)"/> and
    /// <see cref="MindedContext.TryGetScoped{T}(out T)"/> ambient scope primitives covering async flow
    /// propagation, parallel isolation, nesting semantics and disposal behaviour.
    /// </summary>
    [TestClass]
    public class MindedContextScopeTests
    {
        private readonly struct BypassFlag { }

        private MindedContext NewContext() =>
            new MindedContext(Guid.NewGuid(), DateTimeOffset.UtcNow, CancellationToken.None, null);

        [TestMethod]
        public void TryGetScoped_returns_false_when_no_scope_is_active()
        {
            var sut = NewContext();

            var found = sut.TryGetScoped<BypassFlag>(out _);

            found.Should().BeFalse();
        }

        [TestMethod]
        public void BeginScope_makes_value_visible_to_TryGetScoped()
        {
            var sut = NewContext();
            var value = Any.Int();

            using (sut.BeginScope(value))
            {
                var found = sut.TryGetScoped<int>(out var retrieved);

                found.Should().BeTrue();
                retrieved.Should().Be(value);
            }
        }

        [TestMethod]
        public void Disposing_scope_removes_value_from_TryGetScoped()
        {
            var sut = NewContext();
            var scope = sut.BeginScope(new BypassFlag());

            scope.Dispose();

            sut.TryGetScoped<BypassFlag>(out _).Should().BeFalse();
        }

        [TestMethod]
        public void Nested_scopes_of_same_type_expose_top_value_and_unwind_in_LIFO_order()
        {
            var sut = NewContext();

            using (sut.BeginScope(1))
            {
                sut.TryGetScoped<int>(out var outer).Should().BeTrue();
                outer.Should().Be(1);

                using (sut.BeginScope(2))
                {
                    sut.TryGetScoped<int>(out var inner).Should().BeTrue();
                    inner.Should().Be(2);
                }

                sut.TryGetScoped<int>(out var afterInner).Should().BeTrue();
                afterInner.Should().Be(1);
            }

            sut.TryGetScoped<int>(out _).Should().BeFalse();
        }

        [TestMethod]
        public void Scopes_of_different_types_are_independent()
        {
            var sut = NewContext();

            using (sut.BeginScope("text"))
            using (sut.BeginScope(42))
            {
                sut.TryGetScoped<string>(out var s).Should().BeTrue();
                sut.TryGetScoped<int>(out var i).Should().BeTrue();
                s.Should().Be("text");
                i.Should().Be(42);
            }

            sut.TryGetScoped<string>(out _).Should().BeFalse();
            sut.TryGetScoped<int>(out _).Should().BeFalse();
        }

        [TestMethod]
        public void Dispose_is_idempotent()
        {
            var sut = NewContext();
            var scope = sut.BeginScope(new BypassFlag());

            scope.Dispose();
            Action second = () => scope.Dispose();

            second.Should().NotThrow();
            sut.TryGetScoped<BypassFlag>(out _).Should().BeFalse();
        }

        [TestMethod]
        public async Task Scope_flows_into_awaited_continuations()
        {
            var sut = NewContext();

            using (sut.BeginScope(123))
            {
                await Task.Yield();
                sut.TryGetScoped<int>(out var value).Should().BeTrue();
                value.Should().Be(123);
            }
        }

        [TestMethod]
        public async Task Scope_flows_into_TaskRun_continuations()
        {
            var sut = NewContext();

            using (sut.BeginScope(new BypassFlag()))
            {
                var seenInside = await Task.Run(() => sut.TryGetScoped<BypassFlag>(out _));

                seenInside.Should().BeTrue();
            }
        }

        [TestMethod]
        public async Task Scope_opened_after_fork_is_not_observed_by_sibling_branch()
        {
            var sut = NewContext();
            var branchBReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var branchAOpened = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            async Task<bool> BranchA()
            {
                await Task.Yield();
                using (sut.BeginScope(new BypassFlag()))
                {
                    branchAOpened.SetResult(true);
                    await branchBReady.Task;
                    return sut.TryGetScoped<BypassFlag>(out _);
                }
            }

            async Task<bool> BranchB()
            {
                await Task.Yield();
                await branchAOpened.Task;
                var observed = sut.TryGetScoped<BypassFlag>(out _);
                branchBReady.SetResult(true);
                return observed;
            }

            var results = await Task.WhenAll(BranchA(), BranchB());

            results[0].Should().BeTrue("branch A opened the scope and must observe it");
            results[1].Should().BeFalse("branch B forked before A opened the scope and must not observe it");
        }

        [TestMethod]
        public async Task Parallel_branches_each_observe_only_their_own_scope_value()
        {
            var sut = NewContext();

            async Task<int> Branch(int value)
            {
                await Task.Yield();
                using (sut.BeginScope(value))
                {
                    await Task.Delay(5);
                    sut.TryGetScoped<int>(out var seen).Should().BeTrue();
                    return seen;
                }
            }

            var results = await Task.WhenAll(Branch(1), Branch(2), Branch(3));

            results.Should().BeEquivalentTo(new[] { 1, 2, 3 });
            sut.TryGetScoped<int>(out _).Should().BeFalse();
        }

        [TestMethod]
        public async Task Scope_survives_across_multiple_async_hops()
        {
            var sut = NewContext();

            async Task<bool> Inner()
            {
                await Task.Yield();
                await Task.Delay(1);
                return sut.TryGetScoped<BypassFlag>(out _);
            }

            using (sut.BeginScope(new BypassFlag()))
            {
                (await Inner()).Should().BeTrue();
            }
        }

        [TestMethod]
        public void BeginScope_returns_distinct_handles_per_call()
        {
            var sut = NewContext();

            var a = sut.BeginScope(1);
            var b = sut.BeginScope(2);

            a.Should().NotBeSameAs(b);

            b.Dispose();
            a.Dispose();
        }
    }
}
