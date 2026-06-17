using System;
using System.Threading;
using FluentAssertions;

namespace Minded.Extensions.Context.Tests
{
    /// <summary>
    /// Covers disposal semantics for <see cref="MindedContext"/>: single disposal clears state,
    /// repeated disposal is a no-op and does not throw.
    /// </summary>
    [TestClass]
    public class MindedContextDisposalTests
    {
        [TestMethod]
        public void Dispose_clears_items_and_typed_slots()
        {
            var sut = new MindedContext(Guid.NewGuid(), DateTimeOffset.UtcNow, CancellationToken.None, null);
            sut.Items["k"] = "v";
            sut.Set("typed");

            sut.Dispose();

            sut.Items.Should().BeEmpty();
            sut.Get<string>().Should().BeNull();
        }

        [TestMethod]
        public void Dispose_is_idempotent()
        {
            var sut = new MindedContext(Guid.NewGuid(), DateTimeOffset.UtcNow, CancellationToken.None, null);

            sut.Dispose();
            Action secondDispose = () => sut.Dispose();

            secondDispose.Should().NotThrow();
        }

        [TestMethod]
        public void Remove_drops_typed_entry()
        {
            var sut = new MindedContext(Guid.NewGuid(), DateTimeOffset.UtcNow, CancellationToken.None, null);
            sut.Set("value");

            sut.Remove<string>();

            sut.TryGet<string>(out _).Should().BeFalse();
        }
    }
}
