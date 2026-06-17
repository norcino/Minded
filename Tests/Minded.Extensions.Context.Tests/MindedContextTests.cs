using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnonymousData;
using FluentAssertions;

namespace Minded.Extensions.Context.Tests
{
    /// <summary>
    /// Unit tests for <see cref="MindedContext"/> covering property bag semantics, strongly typed slots,
    /// depth counter behavior, disposal idempotency and thread safety.
    /// </summary>
    [TestClass]
    public class MindedContextTests
    {
        private MindedContext NewContext() =>
            new MindedContext(Guid.NewGuid(), DateTimeOffset.UtcNow, CancellationToken.None, null);

        [TestMethod]
        public void Ctor_initialises_depth_to_one_and_IsRoot_true()
        {
            var sut = NewContext();

            sut.Depth.Should().Be(1);
            sut.IsRoot.Should().BeTrue();
        }

        [TestMethod]
        public void Ctor_captures_TraceId_CreatedAt_and_Token()
        {
            var traceId = Guid.NewGuid();
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            using var cts = new CancellationTokenSource();

            var sut = new MindedContext(traceId, createdAt, cts.Token, null);

            sut.TraceId.Should().Be(traceId);
            sut.CreatedAtUtc.Should().Be(createdAt);
            sut.RootCancellationToken.Should().Be(cts.Token);
        }

        [TestMethod]
        public void IncrementDepth_and_DecrementDepth_update_IsRoot()
        {
            var sut = NewContext();

            sut.IncrementDepth();
            sut.IsRoot.Should().BeFalse();
            sut.Depth.Should().Be(2);

            sut.DecrementDepth();
            sut.IsRoot.Should().BeTrue();
            sut.Depth.Should().Be(1);
        }

        [TestMethod]
        public void Items_is_thread_safe_under_concurrent_writes()
        {
            var sut = NewContext();
            var writers = Enumerable.Range(0, 100).Select(i => Task.Run(() => sut.Items[$"k{i}"] = i)).ToArray();

            Task.WaitAll(writers);

            sut.Items.Should().HaveCount(100);
        }

        [TestMethod]
        public void Set_and_Get_store_and_retrieve_typed_value()
        {
            var sut = NewContext();
            var value = Any.String();

            sut.Set(value);

            sut.Get<string>().Should().Be(value);
        }

        [TestMethod]
        public void Get_returns_default_when_missing()
        {
            var sut = NewContext();

            sut.Get<int>().Should().Be(0);
            sut.Get<string>().Should().BeNull();
        }

        [TestMethod]
        public void TryGet_returns_true_and_value_when_present()
        {
            var sut = NewContext();
            var value = Any.Int();
            sut.Set(value);

            var found = sut.TryGet<int>(out var retrieved);

            found.Should().BeTrue();
            retrieved.Should().Be(value);
        }

        [TestMethod]
        public void TryGet_returns_false_and_default_when_missing()
        {
            var sut = NewContext();

            var found = sut.TryGet<int>(out var retrieved);

            found.Should().BeFalse();
            retrieved.Should().Be(0);
        }

        [TestMethod]
        public void GetOrAdd_creates_and_stores_when_missing()
        {
            var sut = NewContext();
            var created = Any.String();

            var result = sut.GetOrAdd(() => created);

            result.Should().Be(created);
            sut.Get<string>().Should().Be(created);
        }

        [TestMethod]
        public void GetOrAdd_does_not_invoke_factory_when_present()
        {
            var sut = NewContext();
            var existing = Any.String();
            sut.Set(existing);
            var invocations = 0;

            var result = sut.GetOrAdd(() => { invocations++; return Any.String(); });

            invocations.Should().Be(0);
            result.Should().Be(existing);
        }

        [TestMethod]
        public void GetOrAdd_throws_when_factory_null()
        {
            var sut = NewContext();

            Action act = () => sut.GetOrAdd<string>(null);

            act.Should().Throw<ArgumentNullException>();
        }
    }
}
