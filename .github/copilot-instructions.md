# Minded Framework

Minded is an open-source .NET **CQRS + Mediator + Decorator** framework (NuGet packages `Minded.Framework.*` and `Minded.Extensions.*`).

## Repository Layout

| Folder | Contents |
|--------|---------|
| `Framework/` | Core: Mediator runtime, CQRS abstractions, Decorator base classes |
| `Extensions/` | Optional decorators: Validation, Logging, Exception, Retry, Caching, Authorization, Transaction, WebApi, OData, EF Core |
| `Example/` | Reference application: REST API + CQRS + OData + EF Core + Swagger |
| `Tests/` | Unit and integration tests for framework and extensions |

## Core Architecture

- **`IMediator`** dispatches `ICommand<TResult>` and `IQuery<TResult>` to registered handlers, resolved from `IServiceProvider` using a compiled delegate cache.
- **Decorator chain**: cross-cutting concerns are applied as decorator layers. Registered via `MindedBuilder`; **first registered = innermost** (runs last, closest to handler); **last registered = outermost** (runs first).
- **CQRS**: Commands mutate state; Queries read state. They never cross-call within a handler.
- **Opt-in attributes**: decorators only activate when the corresponding attribute is present on the command/query class (e.g. `[ValidateCommand]`, `[RetryCommand]`, `[MemoryCache]`).

## Key Packages

| Package | Role |
|---------|------|
| `Minded.Framework.Mediator` | Mediator runtime and handler scanning |
| `Minded.Framework.CQRS.Abstractions` | `ICommand`, `IQuery`, handler interfaces |
| `Minded.Framework.Decorator` | `CommandHandlerDecoratorBase`, `QueryHandlerDecoratorBase` |
| `Minded.Extensions.Validation` | Validation decorator + `ICommandValidator<T>` |
| `Minded.Extensions.Logging` | Structured logging decorator + `ILoggable` |
| `Minded.Extensions.Exception` | Exception handling decorator |
| `Minded.Extensions.Retry` | Retry decorator + `[RetryCommand]` |
| `Minded.Extensions.Caching.Memory` | In-memory cache decorator + `[MemoryCache]` |
| `Minded.Extensions.Transaction` | Transaction decorator + `[TransactionalCommand]` |
| `Minded.Extensions.WebApi` | `IRestMediator` for REST controllers |
| `Minded.Extensions.OData` | OData query integration |

## Active Instruction Files

- **`Example/**/*.cs`** → `.github/instructions/minded-backend.instructions.md` — CQRS patterns, validators, REST mediator, test tiers for the Example application.
- **`Framework/**/*.cs`, `Extensions/**/*.cs`, `Tests/**/*.cs`** → `.github/instructions/minded-contributing.instructions.md` — how to add new framework extensions, coding conventions, package structure.

## General Principles

1. All public APIs in `Framework/` and `Extensions/` require XML documentation (`/// <summary>`).
2. Target frameworks: `netstandard2.0;net8.0;net10.0`.
3. Tests use MSTest + Moq + FluentAssertions.
4. Never put validation or cross-cutting logic inside a handler — use the decorator pipeline.
5. Exception handling must be the outermost decorator (registered last) to catch all errors.
