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
    /// Tests for <see cref="ContextCommandHandlerDecorator{TCommand}"/> covering root context creation,
    /// nested reuse, ITraceable propagation, disposal and exception handling.
    /// </summary>
    [TestClass]
    public class ContextCommandHandlerDecoratorTests
    {
        private MindedContextAccessor _accessor;
        private Mock<IMediator> _mediator;
        private CallbackCommandHandler<NonTraceableCommand> _inner;
        private ContextCommandHandlerDecorator<NonTraceableCommand> _sut;

        [TestInitialize]
        public void Setup()
        {
            _accessor = new MindedContextAccessor();
            _mediator = new Mock<IMediator>();
            _inner = new CallbackCommandHandler<NonTraceableCommand>();
            _sut = new ContextCommandHandlerDecorator<NonTraceableCommand>(_inner, _accessor, _mediator.Object);
        }

        [TestMethod]
        public async Task Root_call_creates_context_with_command_TraceId()
        {
            var command = new NonTraceableCommand();
            IMindedContext observed = null;
            _inner.OnHandle = (_, __) => { observed = _accessor.Current; return Task.FromResult<ICommandResponse>(new CommandResponse(true)); };

            await _sut.HandleAsync(command);

            observed.Should().NotBeNull();
            observed.TraceId.Should().Be(command.TraceId);
            observed.Depth.Should().Be(1);
            observed.IsRoot.Should().BeTrue();
        }

        [TestMethod]
        public async Task Root_call_disposes_and_clears_accessor_on_exit()
        {
            await _sut.HandleAsync(new NonTraceableCommand());

            _accessor.Current.Should().BeSameAs(NullMindedContext.Instance);
        }

        [TestMethod]
        public async Task Nested_call_reuses_parent_context_and_increments_depth()
        {
            var outer = new NonTraceableCommand();
            var inner = new NonTraceableCommand();
            var innerHandler = new CallbackCommandHandler<NonTraceableCommand>();
            var nestedDecorator = new ContextCommandHandlerDecorator<NonTraceableCommand>(innerHandler, _accessor, _mediator.Object);

            IMindedContext ctxOuter = null;
            IMindedContext ctxInner = null;
            var innerDepth = 0;
            innerHandler.OnHandle = (_, __) => { ctxInner = _accessor.Current; innerDepth = ctxInner.Depth; return Task.FromResult<ICommandResponse>(new CommandResponse(true)); };
            _inner.OnHandle = async (_, __) => { ctxOuter = _accessor.Current; await nestedDecorator.HandleAsync(inner); return new CommandResponse(true); };

            await _sut.HandleAsync(outer);

            ctxInner.Should().BeSameAs(ctxOuter);
            innerDepth.Should().Be(2);
            ctxOuter.Depth.Should().Be(1);
        }

        [TestMethod]
        public async Task Nested_ITraceable_command_receives_parent_TraceId()
        {
            var outer = new NonTraceableCommand();
            var innerCmd = new TraceableCommand();
            var originalInnerId = innerCmd.TraceId;
            var innerHandler = new CallbackCommandHandler<TraceableCommand>();
            var nested = new ContextCommandHandlerDecorator<TraceableCommand>(innerHandler, _accessor, _mediator.Object);

            _inner.OnHandle = async (_, __) => { await nested.HandleAsync(innerCmd); return new CommandResponse(true); };

            await _sut.HandleAsync(outer);

            innerCmd.TraceId.Should().Be(outer.TraceId);
            innerCmd.TraceId.Should().NotBe(originalInnerId);
        }

        [TestMethod]
        public async Task Nested_non_ITraceable_command_keeps_own_TraceId()
        {
            var outer = new NonTraceableCommand();
            var innerCmd = new NonTraceableCommand();
            var originalInnerId = innerCmd.TraceId;
            var innerHandler = new CallbackCommandHandler<NonTraceableCommand>();
            var nested = new ContextCommandHandlerDecorator<NonTraceableCommand>(innerHandler, _accessor, _mediator.Object);

            _inner.OnHandle = async (_, __) => { await nested.HandleAsync(innerCmd); return new CommandResponse(true); };

            await _sut.HandleAsync(outer);

            innerCmd.TraceId.Should().Be(originalInnerId);
            innerCmd.TraceId.Should().NotBe(outer.TraceId);
        }

        [TestMethod]
        public async Task Handler_exception_still_disposes_context_and_clears_accessor()
        {
            _inner.OnHandle = (_, __) => throw new InvalidOperationException("boom");

            Func<Task> act = () => _sut.HandleAsync(new NonTraceableCommand());

            await act.Should().ThrowAsync<InvalidOperationException>();
            _accessor.Current.Should().BeSameAs(NullMindedContext.Instance);
        }

        [TestMethod]
        public async Task Root_context_exposes_RootCancellationToken_and_Mediator()
        {
            using var cts = new CancellationTokenSource();
            IMindedContext observed = null;
            _inner.OnHandle = (_, __) => { observed = _accessor.Current; return Task.FromResult<ICommandResponse>(new CommandResponse(true)); };

            await _sut.HandleAsync(new NonTraceableCommand(), cts.Token);

            observed.RootCancellationToken.Should().Be(cts.Token);
            observed.Mediator.Should().BeSameAs(_mediator.Object);
        }
    }
}
