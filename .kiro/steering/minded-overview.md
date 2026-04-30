---
description: Always-loaded overview of the Minded CQRS + Mediator + Decorator framework
globs: []
alwaysApply: true
---

# Minded Framework — Overview

**Minded** is a .NET CQRS + Mediator + Decorator framework. All business operations flow through `IMediator`, which dispatches `ICommand<TResult>` and `IQuery<TResult>` to registered handlers through a configurable decorator chain.

## Repository Layout

| Folder | Contents |
|--------|---------|
| `Framework/` | Mediator runtime, CQRS abstractions, Decorator base classes |
| `Extensions/` | Opt-in decorators: Validation, Logging, Exception, Retry, Caching, Authorization, Transaction, WebApi, OData |
| `Example/` | Reference application (REST API + CQRS + OData + EF Core) |
| `Tests/` | Unit and integration tests |

## Core Concepts

- **Mediator**: `IMediator` resolves handlers by command/query type from DI, using a compiled delegate cache.
- **Decorator chain**: first registered = innermost (runs last, right before handler); last registered = outermost (runs first). Exception handling must be outermost.
- **CQRS**: Commands (`ICommand` / `ICommand<TResult>`) mutate state. Queries (`IQuery<TResult>`) read state.
- **Opt-in attributes**: decorators only activate when the matching attribute is present (`[ValidateCommand]`, `[RetryCommand]`, `[MemoryCache]`, `[TransactionalCommand]`, `[RequirePermissions]`).

## Key Principles

1. Never put validation or cross-cutting logic inside a handler — use the decorator pipeline.
2. Validation must be the innermost decorator (registered first); exception handling must be outermost (registered last).
3. All framework and extension public APIs require XML documentation.
4. Commands must carry `TraceId` for distributed trace correlation.
5. Handlers are thin: one responsibility, no `IMediator` calls for standard CRUD, no inline validation.

## Steering Files in This Repo

- `minded-contributing.md` — rules for extending the framework (apply when editing `Framework/`, `Extensions/`, `Tests/`)
- `minded-utilization.md` — rules for writing application code (apply when editing `Example/`)
