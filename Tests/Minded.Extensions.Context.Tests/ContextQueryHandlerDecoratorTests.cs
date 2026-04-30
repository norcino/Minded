using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Minded.Extensions.Context.Decorator;
using Minded.Framework.CQRS.Command;
using Minded.Framework.Mediator;
using Moq;

namespace Minded.Extensions.Context.Tests
{
    /// <summary>
    /// Tests for <see cref="ContextQueryHandlerDecorator{TQuery,TResult}"/> covering the same semantics
    /// validated for the command decorators: root context creation, nested reuse, ITraceable
    /// propagation, disposal and exception handling.
    /// </summary>
    [TestClass]
    public class ContextQueryHandlerDecoratorTests
    {
        private MindedContextAccessor _accessor;
        private Mock<IMediator> _mediator;
        private CallbackQueryHandler<NonTraceableQuery, string> _inner;
        private ContextQueryHandlerDecorator<NonTraceableQuery, string> _sut;

        [TestInitialize]
        public void Setup()
        {
            _accessor = new MindedContextAccessor();
            _mediator = new Mock<IMediator>();
            _inner = new CallbackQueryHandler<NonTraceableQuery, string>();
            _sut = new ContextQueryHandlerDecorator<NonTraceableQuery, string>(_inner, _accessor, _mediator.Object);
        }

        [TestMethod]
        public async Task Root_query_creates_context_with_query_TraceId()
        {
            var query = new NonTraceableQuery();
            IMindedContext observed = null;
            _inner.OnHandle = (_, __) => { observed = _accessor.Current; return Task.FromResult("r"); };

            await _sut.HandleAsync(query);

            observed.Should().NotBeNull();
            observed.TraceId.Should().Be(query.TraceId);
            observed.Depth.Should().Be(1);
        }

        [TestMethod]
        public async Task Root_query_disposes_and_clears_accessor()
        {
            _inner.OnHandle = (_, __) => Task.FromResult("r");

            await _sut.HandleAsync(new NonTraceableQuery());

            _accessor.Current.Should().BeSameAs(NullMindedContext.Instance);
        }

        [TestMethod]
        public async Task Nested_query_reuses_parent_context()
        {
            var innerQuery = new NonTraceableQuery();
            var innerHandler = new CallbackQueryHandler<NonTraceableQuery, string>();
            var nested = new ContextQueryHandlerDecorator<NonTraceableQuery, string>(innerHandler, _accessor, _mediator.Object);

            IMindedContext outerCtx = null;
            IMindedContext innerCtx = null;
            var innerDepth = 0;
            innerHandler.OnHandle = (_, __) => { innerCtx = _accessor.Current; innerDepth = innerCtx.Depth; return Task.FromResult("inner"); };
            _inner.OnHandle = async (_, __) => { outerCtx = _accessor.Current; await nested.HandleAsync(innerQuery); return "outer"; };

            await _sut.HandleAsync(new NonTraceableQuery());

            innerCtx.Should().BeSameAs(outerCtx);
            innerDepth.Should().Be(2);
            outerCtx.Depth.Should().Be(1);
        }

        [TestMethod]
        public async Task Nested_ITraceable_query_receives_parent_TraceId()
        {
            var outer = new NonTraceableQuery();
            var innerQuery = new TraceableQuery();
            var innerHandler = new CallbackQueryHandler<TraceableQuery, string>();
            var nested = new ContextQueryHandlerDecorator<TraceableQuery, string>(innerHandler, _accessor, _mediator.Object);

            innerHandler.OnHandle = (_, __) => Task.FromResult("x");
            _inner.OnHandle = async (_, __) => { await nested.HandleAsync(innerQuery); return "y"; };

            await _sut.HandleAsync(outer);

            innerQuery.TraceId.Should().Be(outer.TraceId);
        }

        [TestMethod]
        public async Task Handler_exception_still_disposes_context()
        {
            _inner.OnHandle = (_, __) => throw new InvalidOperationException("boom");

            Func<Task> act = () => _sut.HandleAsync(new NonTraceableQuery());

            await act.Should().ThrowAsync<InvalidOperationException>();
            _accessor.Current.Should().BeSameAs(NullMindedContext.Instance);
        }
    }
}
