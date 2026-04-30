using System;
using FluentAssertions;

namespace Minded.Extensions.Context.Tests
{
    /// <summary>
    /// Verifies the no-op behavior of <see cref="NullMindedContext"/> returned by the accessor when no
    /// mediator invocation is in progress.
    /// </summary>
    [TestClass]
    public class NullMindedContextTests
    {
        [TestMethod]
        public void Instance_is_shared_singleton()
        {
            NullMindedContext.Instance.Should().BeSameAs(NullMindedContext.Instance);
        }

        [TestMethod]
        public void Members_return_empty_or_default_values()
        {
            var sut = NullMindedContext.Instance;

            sut.TraceId.Should().Be(Guid.Empty);
            sut.Depth.Should().Be(0);
            sut.IsRoot.Should().BeFalse();
            sut.Mediator.Should().BeNull();
            sut.Items.Should().BeEmpty();
        }

        [TestMethod]
        public void Mutations_are_no_ops()
        {
            var sut = NullMindedContext.Instance;

            sut.Set("something");
            sut.TryGet<string>(out var retrieved).Should().BeFalse();
            retrieved.Should().BeNull();
            sut.Get<string>().Should().BeNull();
        }

        [TestMethod]
        public void GetOrAdd_invokes_factory_but_does_not_store()
        {
            var sut = NullMindedContext.Instance;

            var result = sut.GetOrAdd(() => "value");

            result.Should().Be("value");
            sut.Get<string>().Should().BeNull();
        }

        [TestMethod]
        public void Dispose_does_not_throw()
        {
            Action act = () => NullMindedContext.Instance.Dispose();

            act.Should().NotThrow();
        }

        [TestMethod]
        public void BeginScope_returns_disposable_that_does_not_throw()
        {
            var sut = NullMindedContext.Instance;

            var scope = sut.BeginScope(42);

            scope.Should().NotBeNull();
            Action dispose = () => scope.Dispose();
            dispose.Should().NotThrow();
            dispose.Should().NotThrow();
        }

        [TestMethod]
        public void TryGetScoped_returns_false_even_while_scope_is_open()
        {
            var sut = NullMindedContext.Instance;

            using (sut.BeginScope(42))
            {
                sut.TryGetScoped<int>(out var value).Should().BeFalse();
                value.Should().Be(0);
            }
        }
    }
}
