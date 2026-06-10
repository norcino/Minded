# Minded Framework

Minded is a .NET **CQRS + Mediator + Decorator** framework (NuGet: `Minded.Framework.*`, `Minded.Extensions.*`).

## Repository Layout

| Folder | Contents |
|--------|---------|
| `Framework/` | Mediator runtime, CQRS abstractions, Decorator base classes |
| `Extensions/` | Opt-in decorators: Validation, Logging, Exception, Retry, Caching, Authorization, Transaction, WebApi, OData, EF Core |
| `Example/` | Reference application: REST API + CQRS + OData + EF Core |
| `Tests/` | Unit and integration tests |

## Core Architecture

- **`IMediator`** dispatches `ICommand<TResult>` / `IQuery<TResult>` to registered handlers using a compiled delegate cache.
- **Decorator chain**: cross-cutting concerns applied as decorator layers via `MindedBuilder`.
  - **First registered = innermost** (runs last, right before the handler).
  - **Last registered = outermost** (runs first, intercepts earliest).
- **CQRS**: Commands mutate state; Queries read state.
- **Opt-in attributes**: decorators only activate when their attribute is present (`[ValidateCommand]`, `[RetryCommand]`, `[MemoryCache]`, `[TransactionalCommand]`, `[RequirePermissions]`).

## Path-Scoped Rules

Detailed rules load automatically when you edit files in these areas:

| Editing | Rules loaded |
|---------|-------------|
| `Framework/**`, `Extensions/**`, `Tests/**` | `.claude/rules/minded-contributing.md` |
| `Example/**` | `.claude/rules/minded-utilization.md` |

## Core Invariants (Always Apply)

- `[MemoryCache]` and `IGenerateCacheKey` must **always** be used together on the same query.
- `[RetryCommand]` applies **only** to idempotent operations (never financial debits, email sends, etc.).
- Exception decorator must be **last registered** (outermost) to catch all errors.
- No validation inside handlers — use `[ValidateCommand]` + `ICommandValidator<T>`.
- No `IMediator` calls inside read only OData controllers.
- Inside handlers use `IMediator` to call other command handlers or query handlers to respect single responsibility principle.
- Entities loaded inside Validators are available inside Handlers through `IMindedContextAccessor` and `ICurrentContext.Items` dictionary.
- All public APIs in `Framework/` and `Extensions/` require XML documentation (`/// <summary>`).
- All I/O via `async/await`; never `.Result` or `.Wait()`.