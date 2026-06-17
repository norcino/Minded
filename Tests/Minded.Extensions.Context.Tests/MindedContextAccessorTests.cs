using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;

namespace Minded.Extensions.Context.Tests
{
    /// <summary>
    /// Tests for <see cref="MindedContextAccessor"/>: null fallback to <see cref="NullMindedContext"/>,
    /// round-trip of the internal slot and <see cref="AsyncLocal{T}"/> flow across awaits and parallel
    /// tasks.
    /// </summary>
    [TestClass]
    public class MindedContextAccessorTests
    {
        [TestMethod]
        public void Current_returns_NullMindedContext_when_not_initialised()
        {
            var sut = new MindedContextAccessor();

            sut.Current.Should().BeSameAs(NullMindedContext.Instance);
        }

        [TestMethod]
        public void Current_returns_published_context()
        {
            var sut = new MindedContextAccessor();
            var context = new MindedContext(Guid.NewGuid(), DateTimeOffset.UtcNow, CancellationToken.None, null);
            sut.InternalCurrent = context;

            sut.Current.Should().BeSameAs(context);
        }

        [TestMethod]
        public async Task Current_flows_across_awaits()
        {
            var sut = new MindedContextAccessor();
            var context = new MindedContext(Guid.NewGuid(), DateTimeOffset.UtcNow, CancellationToken.None, null);
            sut.InternalCurrent = context;

            await Task.Yield();
            await Task.Delay(5);

            sut.Current.Should().BeSameAs(context);
        }

        [TestMethod]
        public async Task Current_flows_into_TaskRun()
        {
            var sut = new MindedContextAccessor();
            var context = new MindedContext(Guid.NewGuid(), DateTimeOffset.UtcNow, CancellationToken.None, null);
            sut.InternalCurrent = context;

            IMindedContext captured = null;
            await Task.Run(() => captured = sut.Current);

            captured.Should().BeSameAs(context);
        }

        [TestMethod]
        public async Task Current_is_isolated_per_parallel_branch_when_branches_publish_their_own()
        {
            var sut = new MindedContextAccessor();

            async Task<Guid> Branch()
            {
                var local = new MindedContext(Guid.NewGuid(), DateTimeOffset.UtcNow, CancellationToken.None, null);
                sut.InternalCurrent = local;
                await Task.Delay(10);
                return sut.Current.TraceId;
            }

            var ids = await Task.WhenAll(Branch(), Branch(), Branch());

            ids.Should().OnlyHaveUniqueItems();
        }
    }
}
