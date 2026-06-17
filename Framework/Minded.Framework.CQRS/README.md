# Minded.Framework.CQRS

Core CQRS implementation containing the concrete response types (`CommandResponse`, `QueryResponse`), the `OutcomeEntry` detail type, the handler registration extensions and the default logging sanitization pipeline.

## Features

- **CommandResponse / CommandResponse&lt;TResult&gt;** - Concrete command responses with static `Success`/`Error` factories and fluent outcome chaining
- **QueryResponse&lt;TResult&gt;** - Concrete query response with the same factory and chaining surface
- **OutcomeEntry** - Constructor-based outcome detail type for validation and business rule results
- **Handler Registration** - `AddCommandHandlers()` / `AddQueryHandlers()` extensions for the `MindedBuilder`
- **Logging Sanitization Pipeline** - Default `ILoggingSanitizerPipeline` implementation used by logging and exception decorators

## Installation

```bash
dotnet add package Minded.Framework.CQRS
```

## CommandResponse

`CommandResponse` (namespace `Minded.Framework.CQRS.Command`) implements `ICommandResponse` and exposes `bool Successful` and `List<IOutcomeEntry> OutcomeEntries`.

**Constructors:**

```csharp
public CommandResponse();                                                    // empty outcome list
public CommandResponse(params IOutcomeEntry[] outcomeEntries);               // Successful defaults to false
public CommandResponse(bool successful, params IOutcomeEntry[] outcomeEntries);
```

**Static factories:**

```csharp
public static CommandResponse Success(params IOutcomeEntry[] outcomeEntries); // Successful = true
public static CommandResponse Error(params IOutcomeEntry[] outcomeEntries);   // Successful = false
```

**Fluent chaining:**

```csharp
public CommandResponse WithOutcome(IOutcomeEntry entry);
public CommandResponse WithOutcomes(params IOutcomeEntry[] entries);
```

## CommandResponse&lt;TResult&gt;

`CommandResponse<TResult>` extends `CommandResponse` and implements `ICommandResponse<TResult>`. `Result` is read-only and can only be set through a constructor.

**Constructors:**

```csharp
public CommandResponse();
public CommandResponse(params IOutcomeEntry[] outcomeEntries);               // Successful defaults to false
public CommandResponse(bool successful, params IOutcomeEntry[] outcomeEntries);
public CommandResponse(TResult result);                                      // sets Successful = true automatically
public CommandResponse(TResult result, bool successful);
public CommandResponse(TResult result, bool successful, params IOutcomeEntry[] outcomeEntries);
```

> Note: `new CommandResponse<T>(result)` sets `Successful = true` automatically — no object initializer needed.

**Static factories:**

```csharp
public static CommandResponse<TResult> Success(TResult result, params IOutcomeEntry[] outcomeEntries);
public static CommandResponse<TResult> Error(params IOutcomeEntry[] outcomeEntries);
```

**Fluent chaining** (returns `CommandResponse<TResult>` for continued chaining):

```csharp
public CommandResponse<TResult> WithOutcome(IOutcomeEntry entry);
public CommandResponse<TResult> WithOutcomes(params IOutcomeEntry[] entries);
```

## QueryResponse&lt;TResult&gt;

`QueryResponse<TResult>` (namespace `Minded.Framework.CQRS.Query`) implements `IQueryResponse<TResult>` with `TResult Result { get; }` (read-only), `bool Successful { get; set; }` and `List<IOutcomeEntry> OutcomeEntries { get; set; }`.

**Constructors:**

```csharp
public QueryResponse();
public QueryResponse(TResult result);                                        // sets Successful = true automatically
public QueryResponse(params IOutcomeEntry[] outcomeEntries);                 // Successful defaults to false
public QueryResponse(bool successful, params IOutcomeEntry[] outcomeEntries);
public QueryResponse(TResult result, bool successful);
public QueryResponse(TResult result, bool successful, params IOutcomeEntry[] outcomeEntries);
```

**Static factories and chaining:**

```csharp
public static QueryResponse<TResult> Success(TResult result, params IOutcomeEntry[] outcomeEntries);
public static QueryResponse<TResult> Error(params IOutcomeEntry[] outcomeEntries);
public QueryResponse<TResult> WithOutcome(IOutcomeEntry entry);
public QueryResponse<TResult> WithOutcomes(params IOutcomeEntry[] entries);
```

## OutcomeEntry

`OutcomeEntry` (namespace `Minded.Framework.CQRS.Abstractions`) is the concrete `IOutcomeEntry` implementation. `PropertyName`, `Message` and `AttemptedValue` have private setters and must be supplied through a constructor — object-initializer syntax does not work for them:

```csharp
public OutcomeEntry(string propertyName, string message);
public OutcomeEntry(string propertyName, string message, object attemptedValue);
public OutcomeEntry(string propertyName, string message, object attemptedValue,
                    Severity severity = Severity.Info, string errorCode = default);
```

`Severity`, `ErrorCode`, `ResourceName` and `UniqueErrorCode` remain settable properties. `ToString()` returns the `Message`.

```csharp
var entry = new OutcomeEntry(
    nameof(user.Email),
    "Email already exists",
    user.Email,
    Severity.Error,
    "DUPLICATE_EMAIL");
```

## Handler Example

```csharp
using Minded.Framework.CQRS.Command;

public class CreateEntityCommandHandler : ICommandHandler<CreateEntityCommand, Entity>
{
    private readonly IEntityRepository _repository;

    public CreateEntityCommandHandler(IEntityRepository repository)
        => _repository = repository;

    public async Task<ICommandResponse<Entity>> HandleAsync(
        CreateEntityCommand command, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.CreateAsync(command.Entity, cancellationToken);
        return CommandResponse<Entity>.Success(entity);
    }
}
```

## Handler Registration

This package also ships the `MindedBuilder` extensions `AddCommandHandlers()` and `AddQueryHandlers()` (namespace `Minded.Framework.Mediator`). They scan the configured assemblies for `ICommandHandler<TCommand>`, `ICommandHandler<TCommand, TResult>` and `IQueryHandler<TQuery, TResult>` implementations, register them in DI (transient by default) and apply the queued decorator registrations around them. Handlers are always the innermost element of the decorator chain. See the [Minded.Framework.Mediator](https://www.nuget.org/packages/Minded.Framework.Mediator) README for the full registration walk-through.

## Logging Sanitization Pipeline

`LoggingSanitizerPipeline` (namespace `Minded.Framework.CQRS.Sanitization`, internal) is the default implementation of `ILoggingSanitizerPipeline`. It converts commands and queries to dictionaries before logging or exception serialization, then applies all registered `ILoggingSanitizer` implementations in registration order. It is registered as a singleton by the `MindedBuilder` during framework configuration.

Behaviour of the default implementation:

- Recursively inspects public properties and fields up to a **maximum depth of 3** levels
- Truncates collections to a **maximum of 10 items** (appending a `"... (truncated)"` marker)
- Excludes non-serializable types (`CancellationToken`, `Task`, `Stream`, delegates, `Action`/`Func`)
- `ExcludeProperties(Type interfaceType, params string[] memberNames)` excludes members from any object implementing the given interface (e.g. `ExcludeProperties(typeof(ILoggable), "LoggingTemplate")`), using O(1) lookups
- Caches per-type reflection metadata; thread-safe for sanitization (registration methods must only be called at startup)

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Framework.CQRS)
- [Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
