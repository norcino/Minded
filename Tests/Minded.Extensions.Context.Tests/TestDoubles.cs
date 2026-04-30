using System;
using System.Threading;
using System.Threading.Tasks;
using Minded.Extensions.Context;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.Context.Tests
{
    public class NonTraceableCommand : ICommand
    {
        public Guid TraceId { get; } = Guid.NewGuid();
    }

    public class NonTraceableCommandWithResult : ICommand<string>
    {
        public Guid TraceId { get; } = Guid.NewGuid();
    }

    public class TraceableCommand : ICommand, ITraceable
    {
        public Guid TraceId { get; set; } = Guid.NewGuid();
    }

    public class TraceableCommandWithResult : ICommand<string>, ITraceable
    {
        public Guid TraceId { get; set; } = Guid.NewGuid();
    }

    public class NonTraceableQuery : IQuery<string>
    {
        public Guid TraceId { get; } = Guid.NewGuid();
    }

    public class TraceableQuery : IQuery<string>, ITraceable
    {
        public Guid TraceId { get; set; } = Guid.NewGuid();
    }

    /// <summary>
    /// Command handler that exposes a callback so each test can drive the handler body directly.
    /// </summary>
    public class CallbackCommandHandler<TCommand> : ICommandHandler<TCommand> where TCommand : ICommand
    {
        public Func<TCommand, CancellationToken, Task<ICommandResponse>> OnHandle { get; set; } =
            (_, __) => Task.FromResult<ICommandResponse>(new CommandResponse(true));

        public Task<ICommandResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
            => OnHandle(command, cancellationToken);
    }

    public class CallbackCommandHandler<TCommand, TResult> : ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
    {
        public Func<TCommand, CancellationToken, Task<ICommandResponse<TResult>>> OnHandle { get; set; } =
            (_, __) => Task.FromResult<ICommandResponse<TResult>>(new CommandResponse<TResult>(default(TResult), true));

        public Task<ICommandResponse<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
            => OnHandle(command, cancellationToken);
    }

    public class CallbackQueryHandler<TQuery, TResult> : IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        public Func<TQuery, CancellationToken, Task<TResult>> OnHandle { get; set; } =
            (_, __) => Task.FromResult<TResult>(default);

        public Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
            => OnHandle(query, cancellationToken);
    }
}
