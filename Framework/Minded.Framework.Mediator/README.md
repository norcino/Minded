# Minded.Framework.Mediator

Mediator pattern implementation for dispatching commands and queries to their handlers.

## Features

- Automatic handler resolution from the DI container
- Per-type handler caching and compiled-delegate dispatch
- Decorator chain execution
- Dependency injection integration via `MindedBuilder`

## Installation

```bash
dotnet add package Minded.Framework.Mediator
```

## How It Works

The `Mediator` class (namespace `Minded.Framework.Mediator`) implements the `IMediator` interface defined in the [Minded.Framework.Mediator.Abstractions](https://www.nuget.org/packages/Minded.Framework.Mediator.Abstractions) package. For each dispatched command or query it:

1. **Resolves the handler type** — builds the closed generic handler interface (`ICommandHandler<TCommand>`, `ICommandHandler<TCommand, TResult>` or `IQueryHandler<TQuery, TResult>`) for the runtime message type, and caches it per command/query type in a thread-safe `ConcurrentDictionary` to avoid repeated `MakeGenericType()` calls.
2. **Resolves the handler instance** from `IServiceProvider`. If no handler is registered, an `InvalidOperationException` is thrown identifying the missing handler type. Because decorators are registered around the handler interface, the resolved instance is the outermost decorator of the chain.
3. **Invokes the handler through a compiled expression delegate** — a strongly-typed invocation delegate is compiled once per handler type and cached, eliminating dynamic dispatch overhead on subsequent calls.
4. **Falls back to `dynamic` dispatch** when the compiled delegate fails with an `InvalidCastException` — this happens when the resolved handler is a mock or proxy whose type does not match the expected handler interface (e.g. in integration tests).

Additionally, `ProcessCommandAsync<TResult>` guards against handlers returning `null`: in that case it returns a failed `CommandResponse<TResult>` containing an outcome entry with the message "The handler returned a null result".

## Registration

### AddMediator

`AddMediator()` lives in this package (namespace `Minded.Framework.Mediator`) as an extension of `MindedBuilder` (from `Minded.Extensions.Configuration`). It registers `IMediator` to resolve to `Mediator`, with a configurable lifetime that defaults to `ServiceLifetime.Transient`.

### AddCommandHandlers / AddQueryHandlers

`AddCommandHandlers()` and `AddQueryHandlers()` ship in the [Minded.Framework.CQRS](https://www.nuget.org/packages/Minded.Framework.CQRS) package (also in the `Minded.Framework.Mediator` namespace). They scan the assemblies selected by the builder's assembly filter and:

- register every `ICommandHandler<TCommand>`, `ICommandHandler<TCommand, TResult>` and `IQueryHandler<TQuery, TResult>` implementation, both by its handler interface and by its concrete type (transient by default);
- apply all the decorator registrations previously queued by the `Add*Decorator()` extension methods around each handler interface.

Because the decorators are wrapped around the registered handlers, **handlers are always the innermost element of the decorator chain**, regardless of when `AddCommandHandlers()` / `AddQueryHandlers()` are called. Among the decorators, first registered = innermost (runs last, right before the handler) and last registered = outermost (runs first).

```csharp
services.AddMinded(
    configuration,
    assembly => assembly.FullName.StartsWith("YourApp"),
    builder =>
    {
        builder.AddMediator();

        builder.AddCommandValidationDecorator()   // innermost decorator
               .AddCommandLoggingDecorator()
               .AddCommandExceptionDecorator()    // outermost decorator
               .AddCommandHandlers();             // handlers — always innermost

        builder.AddQueryValidationDecorator()
               .AddQueryLoggingDecorator()
               .AddQueryExceptionDecorator()
               .AddQueryHandlers();
    });
```

## Usage

```csharp
public class UserService
{
    private readonly IMediator _mediator;

    public UserService(IMediator mediator) => _mediator = mediator;

    public async Task<User> CreateAsync(string name, string email, CancellationToken cancellationToken = default)
    {
        var response = await _mediator.ProcessCommandAsync(new CreateUserCommand(name, email), cancellationToken);
        return response.Result;
    }

    public async Task<User> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _mediator.ProcessQueryAsync(new GetUserByIdQuery(id), cancellationToken);
}
```

See the [main documentation](https://github.com/norcino/Minded) for comprehensive usage examples.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Framework.Mediator)
- [Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
