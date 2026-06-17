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
    /// Tests for the typed result variant <see cref="ContextCommandHandlerDecorator{TCommand,TResult}"/>
    /// mirroring the scenarios of the non-result variant.
    /// </summary>
    [TestClass]
    public class ContextCommandHandlerDecoratorWithResultTests
    {
        private MindedContextAccessor _accessor;
        private Mock<IMediator> _mediator;
        private CallbackCommandHandler<NonTraceableCommandWithResult, string> _inner;
        private ContextCommandHandlerDecorator<NonTraceableCommandWithResult, string> _sut;

        [TestInitialize]
        public void Setup()
        {
            _accessor = new MindedContextAccessor();
            _mediator = new Mock<IMediator>();
            _inner = new CallbackCommandHandler<NonTraceableCommandWithResult, string>();
            _sut = new ContextCommandHandlerDecorator<NonTraceableCommandWithResult, string>(_inner, _accessor, _mediator.Object);
        }

        [TestMethod]
        public async Task Root_call_creates_context_and_returns_inner_response()
        {
            var command = new NonTraceableCommandWithResult();
            var expected = new CommandResponse<string>("ok", true);
            IMindedContext observed = null;
            _inner.OnHandle = (_, __) => { observed = _accessor.Current; return Task.FromResult<ICommandResponse<string>>(expected); };

            var response = await _sut.HandleAsync(command);

            response.Should().BeSameAs(expected);
            observed.TraceId.Should().Be(command.TraceId);
        }

        [TestMethod]
        public async Task Root_call_disposes_context_on_exit()
        {
            _inner.OnHandle = (_, __) => Task.FromResult<ICommandResponse<string>>(new CommandResponse<string>("v", true));

            await _sut.HandleAsync(new NonTraceableCommandWithResult());

            _accessor.Current.Should().BeSameAs(NullMindedContext.Instance);
        }

        [TestMethod]
        public async Task Nested_call_reuses_parent_context()
        {
            var innerCmd = new NonTraceableCommandWithResult();
            var innerHandler = new CallbackCommandHandler<NonTraceableCommandWithResult, string>();
            var nested = new ContextCommandHandlerDecorator<NonTraceableCommandWithResult, string>(innerHandler, _accessor, _mediator.Object);

            IMindedContext outerCtx = null;
            IMindedContext innerCtx = null;
            innerHandler.OnHandle = (_, __) => { innerCtx = _accessor.Current; return Task.FromResult<ICommandResponse<string>>(new CommandResponse<string>("x", true)); };
            _inner.OnHandle = async (_, __) => { outerCtx = _accessor.Current; await nested.HandleAsync(innerCmd); return new CommandResponse<string>("y", true); };

            await _sut.HandleAsync(new NonTraceableCommandWithResult());

            innerCtx.Should().BeSameAs(outerCtx);
        }

        [TestMethod]
        public async Task Nested_ITraceable_command_with_result_receives_parent_TraceId()
        {
            var outer = new NonTraceableCommandWithResult();
            var innerCmd = new TraceableCommandWithResult();
            var innerHandler = new CallbackCommandHandler<TraceableCommandWithResult, string>();
            var nested = new ContextCommandHandlerDecorator<TraceableCommandWithResult, string>(innerHandler, _accessor, _mediator.Object);

            innerHandler.OnHandle = (_, __) => Task.FromResult<ICommandResponse<string>>(new CommandResponse<string>("x", true));
            _inner.OnHandle = async (_, __) => { await nested.HandleAsync(innerCmd); return new CommandResponse<string>("y", true); };

            await _sut.HandleAsync(outer);

            innerCmd.TraceId.Should().Be(outer.TraceId);
        }

        [TestMethod]
        public async Task Handler_exception_still_disposes_context()
        {
            _inner.OnHandle = (_, __) => throw new InvalidOperationException("boom");

            Func<Task> act = () => _sut.HandleAsync(new NonTraceableCommandWithResult());

            await act.Should().ThrowAsync<InvalidOperationException>();
            _accessor.Current.Should().BeSameAs(NullMindedContext.Instance);
        }
    }
}
