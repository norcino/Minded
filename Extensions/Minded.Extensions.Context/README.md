# Minded.Extensions.Context

Ambient, `AsyncLocal`-based execution context that is created at the outermost mediator entry and shared across every decorator and handler participating in the same command or query processing — including any nested mediator calls. Enables metadata exchange between pipeline stages, automatic `TraceId` propagation, and cross-decorator coordination without manual plumbing.

## Features

- **Ambient context** flowing across async boundaries (`await`, `Task.Run`, `Task.WhenAll`) via `AsyncLocal<T>`
- **Single instance per root call**: nested mediator invocations reuse the outermost context
- **Automatic disposal** when the root call completes, even on exceptions
- **TraceId propagation** for opt-in commands and queries via `ITraceable`
- **String-keyed property bag** (`Items`) for ad-hoc metadata exchange between decorators and handlers
- **Strongly typed slots** (`Set<T>`/`Get<T>`/`GetOrAdd<T>`/`TryGet<T>`/`Remove<T>`) for type-safe storage
- **Flow-scoped values** (`BeginScope<T>`/`TryGetScoped<T>`) for per-logical-call flags that isolate parallel branches
- **Null-object accessor**: `IMindedContextAccessor.Current` never returns `null`
- **Nesting depth tracking** (`Depth`, `IsRoot`) for diagnostics and coordination
- **Root cancellation token** and mediator exposed for deeply nested code

## Installation

```bash
dotnet add package Minded.Extensions.Context
```

## Quick Start

### Registration

Register the context decorators as the **outermost** decorators in the pipeline so every other decorator and the handler observe a populated context:

```csharp
services.AddMinded(Configuration, asm => asm.Name.StartsWith("MindedExample.Application."), builder =>
{
    builder.AddMediator();

    // Register commands and queries context decorators as the outermost layer
    builder.AddContextDecorator();

    builder.AddCommandValidationDecorator()
           .AddCommandLoggingDecorator()
           .AddCommandHandlers();

    builder.AddQueryValidationDecorator()
           .AddQueryLoggingDecorator()
           .AddQueryHandlers();
});
```

The helpers can also be registered individually:

```csharp
builder.AddCommandContextDecorator(); // commands only
builder.AddQueryContextDecorator();   // queries only
```

### Accessing the Context

Inject `IMindedContextAccessor` anywhere and read `Current`:

```csharp
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Order>
{
    private readonly IMindedContextAccessor _contextAccessor;

    public CreateOrderCommandHandler(IMindedContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public Task<ICommandResponse<Order>> HandleAsync(CreateOrderCommand command, CancellationToken ct = default)
    {
        var context = _contextAccessor.Current;
        // context.TraceId, context.Depth, context.IsRoot, context.Items, etc.
        return Task.FromResult(new CommandResponse<Order>(true, command.Order));
    }
}
```

When no mediator invocation is in progress or the decorator is not registered, `Current` returns `NullMindedContext.Instance` (a no-op implementation), so callers never need to null-check.

## IMindedContext Surface

| Member | Description |
|--------|-------------|
| `TraceId` | Correlation id shared across root and nested calls. Seeded from the root command/query when it implements `ITraceable`, otherwise a new `Guid`. |
| `CreatedAtUtc` | UTC timestamp captured at the outermost mediator entry. |
| `Depth` | Current nesting depth. `1` at the root, incremented for each nested mediator call. |
| `IsRoot` | `true` when the current call is the outermost one for this context. |
| `RootCancellationToken` | Cancellation token passed to the outermost mediator call. |
| `Mediator` | Mediator instance that originated the context; lets handlers dispatch nested calls without a direct dependency on `IMediator`. |
| `Items` | Thread-safe `IDictionary<string, object>` for ad-hoc metadata. Values are not disposed with the context. |
| `Set<T>(value)` / `Get<T>()` / `TryGet<T>(out value)` / `GetOrAdd<T>(factory)` / `Remove<T>()` | Strongly typed storage keyed by runtime type. |
| `BeginScope<T>(value)` / `TryGetScoped<T>(out value)` | Ambient, async-flow scoped stack keyed by type. Values are visible to the current logical call and its nested/awaited continuations but not to sibling branches that forked before the scope opened. |

## Metadata Exchange Between Decorators and Handlers

The property bag and the typed slots avoid duplicate work across the pipeline. For example, a validation decorator can load an entity once and make it available to the handler:

```csharp
public class CreateTransactionValidator : ICommandValidator<CreateTransactionCommand>
{
    private readonly IMindedExampleContext _db;
    private readonly IMindedContextAccessor _contextAccessor;

    public async Task<IValidationResult> ValidateAsync(CreateTransactionCommand command)
    {
        var category = await _db.Categories.FindAsync(command.Transaction.CategoryId);
        if (category == null)
            return Fail("CategoryId", "Category not found");

        // Cache the loaded entity for the handler
        _contextAccessor.Current.Set(category);
        return new ValidationResult();
    }
}

public class CreateTransactionCommandHandler : ICommandHandler<CreateTransactionCommand, Transaction>
{
    private readonly IMindedContextAccessor _contextAccessor;

    public Task<ICommandResponse<Transaction>> HandleAsync(CreateTransactionCommand command, CancellationToken ct = default)
    {
        // Reuse the entity already loaded by the validator
        var category = _contextAccessor.Current.Get<Category>();
        command.Transaction.Category = category;
        // ...
    }
}
```

## TraceId Propagation with ITraceable

Commands and queries that implement `ITraceable` have their `TraceId` aligned with the ambient context, so nested invocations automatically share the same correlation id:

```csharp
public class CreateOrderCommand : ICommand<Order>, ITraceable
{
    public Order Order { get; set; }
    public Guid TraceId { get; set; } = Guid.NewGuid();
}
```

When the root command is dispatched, the context is seeded from its `TraceId`. When a handler dispatches nested commands or queries that also implement `ITraceable`, the context decorator overwrites their `TraceId` with the ambient value — no manual propagation required. Commands and queries that do not implement `ITraceable` retain their independently generated `TraceId` and are unaffected.


## Nested Calls and Depth

The context decorator creates a new context only at the outermost mediator entry. Every nested mediator call performed through the same `IMediator` (directly or via `IMindedContext.Mediator`) reuses the existing context and increments `Depth` for the duration of the nested call.

```csharp
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Order>
{
    private readonly IMindedContextAccessor _contextAccessor;

    public async Task<ICommandResponse<Order>> HandleAsync(CreateOrderCommand command, CancellationToken ct = default)
    {
        var context = _contextAccessor.Current;
        // context.Depth == 1, context.IsRoot == true here

        // Nested dispatch — the context is reused, Depth becomes 2 inside the nested handler
        var customer = await context.Mediator.ProcessQueryAsync(
            new GetCustomerByIdQuery(command.Order.CustomerId), ct);

        return new CommandResponse<Order>(true, command.Order);
    }
}
```

`Depth` and `IsRoot` are useful for decorators that need to distinguish between root and nested executions — for example, an authorization decorator that performs policy  checks using commandsonly on the root call and skips them for nested sub-operations to avoid recursion.

## Scoped Values for Loop Prevention

`Items` and the typed slots are shared across every call served by the context, including parallel branches. That makes them unsuitable for per-logical-call flags such as "bypass this decorator for the sub-call I am about to dispatch": setting a flag before dispatching would also affect any sibling branch executing concurrently.

`BeginScope<T>(value)` and `TryGetScoped<T>(out value)` solve this case. Scoped values are stored in an `AsyncLocal<T>`-backed stack keyed by type. A value pushed with `BeginScope` is visible to the current logical call and to anything it spawns (awaited continuations, `Task.Run`, nested mediator dispatches), but is not visible to sibling branches that forked before the scope was opened. Disposing the handle returned by `BeginScope` pops the value.

A typical use case is an authorization decorator whose permission check itself dispatches a command or query. Without scoping, the nested dispatch would re-enter the authorization decorator and loop indefinitely. A scoped bypass marker isolates the sub-invocation:

```csharp
public readonly struct AuthorizationBypass { }

public class AuthorizationCommandHandlerDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _next;
    private readonly IMindedContextAccessor _contextAccessor;
    private readonly IPermissionCheckFactory _factory;

    public async Task<ICommandResponse<TResult>> HandleAsync(TCommand command, CancellationToken ct = default)
    {
        var context = _contextAccessor.Current;

        // Re-entry guard: skip the check when we are already inside a permission-check subtree.
        if (context.TryGetScoped<AuthorizationBypass>(out _))
            return await _next.HandleAsync(command, ct);

        var permissionCheck = _factory.Create(command);
        if (permissionCheck != null)
        {
            using (context.BeginScope(new AuthorizationBypass()))
            {
                var allowed = await context.Mediator.ProcessCommandAsync(permissionCheck, ct);
                if (!allowed.Successful)
                    return new CommandResponse<TResult>(false);
            }
        }

        // The scope is disposed here: the real handler and any legitimate sub-commands it dispatches
        // will be authorized normally.
        return await _next.HandleAsync(command, ct);
    }
}
```

Concurrency semantics:

```csharp
// Handler dispatches two commands in parallel. Each one enters the authorization decorator independently.
await Task.WhenAll(
    _mediator.ProcessCommandAsync(commandA, ct),   // auth decorator opens a bypass scope for its own sub-check
    _mediator.ProcessCommandAsync(commandB, ct)    // unaffected: its auth decorator does not observe A's scope
);
```

Rules of use:

- Dispose the handle returned by `BeginScope` in the same async flow that created it. Using a `using` block is the recommended pattern.
- Scopes nest LIFO. An inner scope for the same type hides the outer value until it is disposed, then the outer value becomes visible again.
- `TryGetScoped<T>` returns `false` when called outside any active scope for that type, or on `NullMindedContext`.
- Values stored in scopes are not intended for cross-branch communication. Use `Items` or typed slots for that.

## Storage vs Scopes: Lifetime and Disposal

The context offers three distinct storage shapes, each with its own lifetime and disposal rules. Pick the one that matches how the value should live.

| Storage | Visibility | Lifetime | Who calls `Dispose()`? |
|---------|------------|----------|------------------------|
| `Items["key"] = value` | Whole context (shared across parallel branches) | Until the root call completes | **You do.** The context only drops the reference. |
| `Set<T>(value)` / `GetOrAdd<T>(factory)` | Whole context (shared across parallel branches) | Until the root call completes | **You do.** The context only drops the reference. |
| `BeginScope<T>(value)` | Current async flow and its descendants | Until the returned handle is disposed | The context pops the scope; **the stored value itself is not disposed** unless you also own its lifetime elsewhere. |

### What "the context does not dispose values" means

When the root call completes, `MindedContext.Dispose()` clears `Items` and the typed slots. It does not invoke `Dispose()` or `DisposeAsync()` on any value it held, even if the value implements `IDisposable`.

**Why this design:** storage is for data exchange between pipeline stages. The context does not know who owns the value (it may be a DI‑scoped instance, a cached entity, or a disposable owned by the caller), so it deliberately avoids assuming ownership. This prevents double‑dispose bugs and surprises when values are shared.

### Garbage collection is not disposal

If you add an `IDisposable` to `Items` and forget to dispose it, after the context releases the reference the object becomes eligible for GC when no other references remain. The managed memory will be reclaimed, but:

- `Dispose()` will **not** be called.
- Any **unmanaged** resources the object holds (file handles, sockets, DB connections) remain allocated until either an explicit `Dispose()` call, or a finalizer on the type eventually runs during GC — which is non‑deterministic and not guaranteed for types without a finalizer.

Treat GC as a memory reclamation mechanism, not a disposal mechanism. Deterministic cleanup is your responsibility.

### Guidance

- Prefer POCOs, entities, and DTOs in `Items` and typed slots. Values with no cleanup requirement are the safe case.
- Do not put short‑lived `IDisposable` resources in the context if you can avoid it. Own them in the stage that created them, inside a `using` or `try`/`finally`.
- If you must share an `IDisposable` across stages, pick a single owning stage and dispose it there explicitly once downstream stages are done.
- `BeginScope<T>` returns an `IDisposable` handle whose disposal pops the scope. That handle is the one you put inside a `using`. This `using` does **not** dispose the value you pushed into the scope — only the scope frame.
- The validator‑caches‑entity pattern shown in [Metadata Exchange Between Decorators and Handlers](#metadata-exchange-between-decorators-and-handlers) is safe: EF entities are plain objects, not `IDisposable`, so the handler does not need to clean anything up.

## Lifecycle

1. The outermost mediator call enters the context decorator.
2. A fresh `IMindedContext` is created. Its `TraceId` is seeded from the command or query when it implements `ITraceable`, otherwise a new `Guid` is generated. `CreatedAtUtc`, `RootCancellationToken`, and `Mediator` are captured.
3. The accessor publishes the context through its `AsyncLocal<T>` slot so it flows across `await`, `Task.Run`, and `Task.WhenAll`.
4. Nested mediator calls observe the same instance and increment/decrement `Depth` while they execute.
5. When the outermost call completes — successfully or with an exception — the context is disposed and the accessor is cleared.

Disposal clears `Items`, typed slots, and any remaining scope state. See [Storage vs Scopes: Lifetime and Disposal](#storage-vs-scopes-lifetime-and-disposal) for what the context does and does not dispose on stored values.

## Parallel Execution

The context is thread-safe and flows correctly into parallel branches:

```csharp
var context = _contextAccessor.Current;

await Task.WhenAll(
    Task.Run(() => DoWork(context)),   // sees the same context
    Task.Run(() => DoWork(context))
);
```

All mutations to `Items` and the typed slots are synchronized internally, so concurrent writes from parallel branches do not require additional locking.

## Interaction with Framework Projects

The framework core (`Minded.Framework.*`) has no dependency on this extension. Adding the package and calling `AddContextDecorator` opts into the behaviour without changing any existing command, query, handler, or decorator signature. Removing the package and the registration reverts the application to its previous behaviour.

## Complete Example

```csharp
services.AddMinded(Configuration, asm => asm.Name.StartsWith("MindedExample.Application."), builder =>
{
    builder.AddMediator();

    // Establish the ambient context at the outermost layer of both pipelines
    builder.AddContextDecorator();

    builder.AddCommandValidationDecorator()
           .AddCommandExceptionDecorator()
           .AddCommandRetryDecorator()
           .AddCommandLoggingDecorator()
           .AddCommandHandlers();

    builder.AddQueryValidationDecorator()
           .AddQueryExceptionDecorator()
           .AddQueryLoggingDecorator()
           .AddQueryHandlers();
});

// Opt-in command: TraceId is aligned with the ambient context
public class CreateOrderCommand : ICommand<Order>, ITraceable, ILoggable
{
    public Order Order { get; set; }
    public Guid TraceId { get; set; } = Guid.NewGuid();

    public string LoggingTemplate => "CustomerId: {CustomerId}";
    public string[] LoggingProperties => new[] { "Order.CustomerId" };
}
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.Context)
- [Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)

