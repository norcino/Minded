using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Minded.Extensions.Context.Decorator;
using Minded.Framework.CQRS.Command;
using Minded.Framework.Mediator;
using Moq;

namespace Minded.Extensions.Context.Tests
{
    /// <summary>
    /// End-to-end coverage of the <see cref="System.Threading.AsyncLocal{T}"/> flow: parallel execution
    /// of nested mediator calls under <c>Task.WhenAll</c> observes the same context, while independent
    /// root invocations remain isolated.
    /// </summary>
    [TestClass]
    public class ContextAsyncFlowTests
    {
        [TestMethod]
        public async Task Parallel_nested_calls_observe_same_context()
        {
            var accessor = new MindedContextAccessor();
            var mediator = new Mock<IMediator>().Object;
            var observed = new ConcurrentBag<IMindedContext>();

            var innerHandler = new CallbackCommandHandler<NonTraceableCommand>
            {
                OnHandle = async (_, __) => { await Task.Delay(5); observed.Add(accessor.Current); return new CommandResponse(true); }
            };
            var innerDecorator = new ContextCommandHandlerDecorator<NonTraceableCommand>(innerHandler, accessor, mediator);

            var outerHandler = new CallbackCommandHandler<NonTraceableCommand>
            {
                OnHandle = async (_, __) =>
                {
                    await Task.WhenAll(
                        innerDecorator.HandleAsync(new NonTraceableCommand()),
                        innerDecorator.HandleAsync(new NonTraceableCommand()),
                        innerDecorator.HandleAsync(new NonTraceableCommand()));
                    return new CommandResponse(true);
                }
            };
            var outerDecorator = new ContextCommandHandlerDecorator<NonTraceableCommand>(outerHandler, accessor, mediator);

            await outerDecorator.HandleAsync(new NonTraceableCommand());

            observed.Should().HaveCount(3);
            observed.Select(c => c.TraceId).Distinct().Should().HaveCount(1);
        }

        [TestMethod]
        public async Task Independent_root_invocations_use_distinct_contexts()
        {
            var accessor = new MindedContextAccessor();
            var mediator = new Mock<IMediator>().Object;
            var handler = new CallbackCommandHandler<NonTraceableCommand>();
            var decorator = new ContextCommandHandlerDecorator<NonTraceableCommand>(handler, accessor, mediator);
            var observed = new ConcurrentBag<System.Guid>();

            handler.OnHandle = async (_, __) =>
            {
                await Task.Delay(5);
                observed.Add(accessor.Current.TraceId);
                return new CommandResponse(true);
            };

            await Task.WhenAll(
                decorator.HandleAsync(new NonTraceableCommand()),
                decorator.HandleAsync(new NonTraceableCommand()),
                decorator.HandleAsync(new NonTraceableCommand()));

            observed.Should().HaveCount(3);
            observed.Distinct().Should().HaveCount(3);
            accessor.Current.Should().BeSameAs(NullMindedContext.Instance);
        }
    }
}
