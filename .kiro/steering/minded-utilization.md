---
description: Rules for writing Commands, Queries, Handlers, Validators and controllers using the Minded framework in the Example application
globs:
  - "Example/**"
alwaysApply: false
---

# Minded — Utilization Rules

> Full standalone guide (copy to consuming repos): `AI/minded-utilization.md`

## Project Structure

```
Application/
  Features/{Feature}/
    Commands/           # ICommand / ICommand<TResult> definitions
    CommandHandlers/    # ICommandHandler<TCommand, TResult> implementations
    Queries/            # IQuery<TResult> definitions
    QueryHandlers/      # IQueryHandler<TQuery, TResult> implementations
    Validators/         # ICommandValidator<T>, IQueryValidator<T>, IValidator<TEntity>
```

## Commands

```csharp
[ValidateCommand]
[RetryCommand]          // ONLY for idempotent operations
public class CreateEntityCommand : ICommand<Entity>, ILoggable
{
    public CreateEntityCommand(Entity entity, Guid? traceId = null, CancellationToken ct = default)
    {
        Entity = entity;
        TraceId = traceId ?? Guid.NewGuid();
        CancellationToken = ct;
    }

    public Entity Entity { get; }
    public Guid TraceId { get; }
    public CancellationToken CancellationToken { get; }

    public string LoggingTemplate => "EntityName {EntityName}";
    public string[] LoggingProperties => new[] { "Entity.Name" };
}
```

- Always include `TraceId` (default `Guid.NewGuid()`).
- Implement `ILoggable` for structured logging; `ITraceable` for trace propagation.
- Never apply `[RetryCommand]` to non-idempotent operations.
- Commands must be immutable after construction.

## Command Handlers

```csharp
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

- One responsibility: execute the business action only.
- No validation, no `IMediator` calls (standard CRUD handlers).
- Return `CommandResponse<TResult>.Success(result)` or `CommandResponse<TResult>.Error(new OutcomeEntry(...))` (`new CommandResponse<TResult>(result)` also sets `Successful = true`).

## Queries (OData collection example)

```csharp
[ValidateQuery]
public class GetEntitiesQuery :
    IQuery<IQueryResponse<IEnumerable<Entity>>>,
    ICanCount, ICanTop, ICanSkip, ICanOrderBy, ICanFilterExpression<Entity>, ILoggable
{
    public bool Count { get; set; }
    public int? Top { get; set; }
    public int? Skip { get; set; }
    public string[] Expand { get; set; }
    public IList<OrderDescriptor> OrderBy { get; set; }
    public Expression<Func<Entity, bool>> Filter { get; set; }
    public Guid TraceId { get; } = Guid.NewGuid();
    public string LoggingTemplate => "Top: {Top} Skip: {Skip}";
    public string[] LoggingProperties => new[] { nameof(Top), nameof(Skip) };
}
```

- Queries **must never** modify state.
- Use `[MemoryCache(ExpirationInSeconds = N)]` + `IGenerateCacheKey` **together** for cacheable queries.
- Navigation properties are not auto-loaded — require explicit `$expand`.

## Validators

```csharp
public class CreateEntityCommandValidator : ICommandValidator<CreateEntityCommand>
{
    public async Task<IValidationResult> ValidateAsync(CreateEntityCommand command)
    {
        var result = new ValidationResult();
        if (command.Entity == null)
        {
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(command.Entity), "{0} is required",
                attemptedValue: null, Severity.Error, GenericErrorCodes.ValidationFailed));
            return result;
        }
        // further checks ...
        return result;
    }
}
```

- Return early after a null guard to avoid null-reference cascades.
- Reuse entity validators (`IValidator<TEntity>`) across command validators.

## DI Registration (innermost first, outermost last)

```csharp
builder.AddCommandValidationDecorator()   // innermost
       .AddCommandRetryDecorator()
       .AddCommandLoggingDecorator()
       .AddCommandExceptionDecorator()    // outermost: catches all exceptions
       .AddCommandHandlers();

builder.AddQueryValidationDecorator()
       .AddQueryMemoryCacheDecorator()
       .AddQueryLoggingDecorator()
       .AddQueryExceptionDecorator()
       .AddQueryHandlers();
```

## REST Controllers

- Inject **only** `IRestMediator` — never handlers or repositories directly.
- Map HTTP verbs: GET→`GetMany`/`GetSingle`, POST→`CreateWithContent`, PUT→`UpdateWithContent`, PATCH→`PatchWithContent`, DELETE→`Delete`.
- Always accept `CancellationToken cancellationToken = default` in async actions.

## Key Invariants

- `[MemoryCache]` and `IGenerateCacheKey` must always be used together.
- `[RetryCommand]` only on idempotent operations.
- First registered decorator = innermost; last registered = outermost.
- Exception decorator must be outermost (registered last).
