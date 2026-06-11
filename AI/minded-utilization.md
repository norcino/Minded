# Using the Minded Framework

A standalone guide for developers building applications with the **Minded** CQRS + Mediator + Decorator framework.

> This file is designed to be copied to consuming repos as an AI steering file. See `AI/README.md` in the Minded repository for per-tool setup instructions.

---

## NuGet Packages

| Package | Purpose | Required |
|---------|---------|:--------:|
| `Minded.Framework.Mediator` | `IMediator` runtime and handler scanning | ✅ |
| `Minded.Framework.CQRS.Abstractions` | `ICommand`, `IQuery`, handler interfaces | ✅ |
| `Minded.Framework.CQRS` | `CommandResponse`, `QueryResponse` | ✅ |
| `Minded.Extensions.Validation` | `[ValidateCommand]`, `[ValidateQuery]` decorators | Recommended |
| `Minded.Extensions.Logging` | Structured logging via `ILoggable` | Recommended |
| `Minded.Extensions.Exception` | Exception handling decorator | Recommended |
| `Minded.Extensions.Retry` | `[RetryCommand]`, `[RetryQuery]` decorators | Optional |
| `Minded.Extensions.Caching.Memory` | `[MemoryCache]` in-memory cache decorator | Optional |
| `Minded.Extensions.Transaction` | `[TransactionalCommand]` decorator | Optional |
| `Minded.Extensions.Authorization` | `[RequirePermissions]` decorator | Optional |
| `Minded.Extensions.WebApi` | `IRestMediator` for REST controllers | Optional |
| `Minded.Extensions.OData` | OData query integration | Optional |
| `Minded.Extensions.CQRS.OData` | OData trait interfaces for queries | Optional |
| `Minded.Extensions.CQRS.EntityFrameworkCore` | EF Core query utilities (`.ApplyQueryTo()`) | Optional |

---

## Project Structure

Organise the Application project in feature folders:

```
Application/
  Features/
    {Feature}/
      Commands/           # ICommand / ICommand<TResult> definitions
      CommandHandlers/    # ICommandHandler<TCommand, TResult> implementations
      Queries/            # IQuery<TResult> definitions
      QueryHandlers/      # IQueryHandler<TQuery, TResult> implementations
      Validators/         # ICommandValidator<T>, IQueryValidator<TQuery, TResult>, IValidator<TEntity>
```

Namespaces follow the folder path: `{AppName}.Application.Features.{Feature}.Commands`.

---

## Framework Invariants

These rules are enforced by the framework — violations cause runtime errors or silent misbehaviour:

- Every `[ValidateCommand]`-decorated command **must** have a registered `ICommandValidator<TCommand>` — the validation decorator takes the validator as a constructor dependency, so a missing validator fails at runtime with a DI resolution error when the command is dispatched.
- Every `[ValidateQuery]`-decorated query **must** have a registered `IQueryValidator<TQuery, TResult>`.
- `[MemoryCache]` and `IGenerateCacheKey` **must always be used together** on the same query. Never use one without the other.
- `GetCacheKey()` **must** return a string that uniquely identifies the result — include **all** discriminating properties.
- Decorator registration order: **first registered = innermost** (runs last, right before the handler); **last registered = outermost** (runs first). Exception handling must be outermost.

---

## Commands

### Naming

`{Verb}{Noun}Command` — e.g. `CreateCategoryCommand`, `UpdateUserCommand`, `DeleteOrderCommand`

### Structure

```csharp
[ValidateCommand]                     // triggers ICommandValidator<T>; omit only if no validation needed
[RetryCommand]                        // auto-retry — ONLY for idempotent operations
[TransactionalCommand]                // wraps handler in a DB transaction
public class CreateEntityCommand : ICommand<Entity>, ILoggable
{
    public CreateEntityCommand(
        Entity entity,
        Guid? traceId = null,
        CancellationToken cancellationToken = default)
    {
        Entity = entity;
        TraceId = traceId ?? Guid.NewGuid();
        CancellationToken = cancellationToken;
    }

    public Entity Entity { get; }
    public Guid TraceId { get; }
    public CancellationToken CancellationToken { get; }

    // ILoggable — structured logging via template
    public string LoggingTemplate => "EntityName {EntityName}";
    public string[] LoggingProperties => new[] { "Entity.Name" };
}
```

### Rules

- Always include `TraceId` for distributed trace correlation (`Guid.NewGuid()` default).
- Carry `CancellationToken` as a constructor parameter; store as a property.
- Implement `ILoggable` on commands that benefit from structured logging.
- Implement `ITraceable` for explicit distributed trace propagation.
- Apply `[RetryCommand]` **only** to idempotent operations (safe to re-run). Never apply to financial transactions, email sends, or any non-idempotent side-effects.
- Custom retry delays: `[RetryCommand(3, 100, 200, 400)]` — 3 retries at 100 ms, 200 ms, 400 ms.
- Apply `[TransactionalCommand]` when the handler must perform multiple data modifications atomically.
- Commands should be immutable after construction; use read-only properties.

---

## Command Handlers

### Naming

`{CommandName}Handler` — e.g. `CreateCategoryCommandHandler`

### Structure

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

### Rules

- **One responsibility**: execute the business action only. No validation, no orchestration.
- **No validation inside handlers** — validation belongs in `ICommandValidator<T>`.
- **Handlers should not call `IMediator`** for standard CRUD operations. Keep handlers thin.
- Return `CommandResponse<TResult>.Success(result)` on success; `CommandResponse<TResult>.Error(new OutcomeEntry(...))` on known business failures. (`new CommandResponse<TResult>(result)` is equivalent to `Success` — the result constructor sets `Successful = true`.)
- All I/O must use `async/await`; never `.Result` or `.Wait()`.
- Inject only what is directly needed (repositories, domain services, loggers).

> **Orchestration exception**: When a parent command handler must coordinate multiple sub-operations (saga/orchestrator pattern), injecting `IMediator` is acceptable. Document the reason with an XML comment on the handler class.

---

## Queries

### Naming

`Get{Noun}[Suffix]Query` — e.g. `GetCategoryByIdQuery`, `GetCategoriesQuery`

### Single-item query

```csharp
[ValidateQuery]
[MemoryCache(ExpirationInSeconds = 300)]    // must pair with IGenerateCacheKey
public class GetEntityByIdQuery : IQuery<Entity>, ILoggable, IGenerateCacheKey
{
    public GetEntityByIdQuery(int id, Guid? traceId = null)
    {
        Id = id;
        TraceId = traceId ?? Guid.NewGuid();
    }

    public int Id { get; }
    public Guid TraceId { get; }

    public string LoggingTemplate => "Id {Id}";
    public string[] LoggingProperties => new[] { nameof(Id) };

    public string GetCacheKey() => $"Entity_{Id}";   // include ALL discriminating properties
}
```

### OData collection query

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

    public GetEntitiesQuery(Guid? traceId = null) { TraceId = traceId ?? Guid.NewGuid(); }

    public Guid TraceId { get; }
    public string LoggingTemplate => "Count: {Count} Top: {Top} Skip: {Skip}";
    public string[] LoggingProperties => new[] { nameof(Count), nameof(Top), nameof(Skip) };
}
```

### Rules

- Queries **must never** modify state.
- Implement OData trait interfaces on collection queries: `ICanCount`, `ICanTop`, `ICanSkip`, `ICanOrderBy`, `ICanFilterExpression<T>`, `ICanExpand`.
- Use `[MemoryCache]` + `IGenerateCacheKey` together for frequently-read stable data.
- Use `SlidingExpiration` for user-session-scoped data; absolute `ExpirationInSeconds` for reference data.

---

## Query Handlers

```csharp
// Single-item
public class GetEntityByIdQueryHandler : IQueryHandler<GetEntityByIdQuery, Entity>
{
    private readonly IEntityRepository _repository;

    public GetEntityByIdQueryHandler(IEntityRepository repository)
        => _repository = repository;

    public async Task<Entity> HandleAsync(
        GetEntityByIdQuery query, CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(query.Id, cancellationToken);
}

// OData collection (EF Core)
public class GetEntitiesQueryHandler
    : IQueryHandler<GetEntitiesQuery, IQueryResponse<IEnumerable<Entity>>>
{
    private readonly IAppDbContext _context;

    public GetEntitiesQueryHandler(IAppDbContext context) => _context = context;

    public async Task<IQueryResponse<IEnumerable<Entity>>> HandleAsync(
        GetEntitiesQuery query, CancellationToken cancellationToken = default)
    {
        var result = await query
            .ApplyQueryTo(_context.Entities.AsNoTracking().AsQueryable())
            .ToListAsync(cancellationToken);

        return new QueryResponse<IEnumerable<Entity>>(result);
    }
}
```

### Rules

- **Never modify state** in a query handler.
- Use `.AsNoTracking()` for read-only EF Core queries.
- Navigation properties are **not** loaded by default — require explicit `$expand` via `ICanExpand`.
- Apply OData traits using `.ApplyQueryTo()` before materialising results.

---

## Validators

### Command Validator

```csharp
public class CreateEntityCommandValidator : ICommandValidator<CreateEntityCommand>
{
    private readonly IValidator<Entity> _entityValidator;

    public CreateEntityCommandValidator(IValidator<Entity> entityValidator)
        => _entityValidator = entityValidator;

    public async Task<IValidationResult> ValidateAsync(CreateEntityCommand command)
    {
        var result = new ValidationResult();

        if (command.Entity == null)
        {
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(command.Entity), "{0} is required",
                attemptedValue: null, Severity.Error, GenericErrorCodes.ValidationFailed));
            return result;  // early return — avoid null-reference cascade
        }

        return (await _entityValidator.ValidateAsync(command.Entity)).Merge(result);
    }
}
```

### Entity Validator

```csharp
public class EntityValidator : IValidator<Entity>
{
    public Task<IValidationResult> ValidateAsync(Entity entity)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(entity.Name))
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(entity.Name), "{0} cannot be empty",
                entity.Name, Severity.Error, GenericErrorCodes.ValidationFailed));

        return Task.FromResult<IValidationResult>(result);
    }
}
```

### Rules

- Create separate validators: `ICommandValidator<T>`, `IQueryValidator<T, TResult>`, `IValidator<TEntity>`.
- The validation logic inside a validator may use any library (FluentValidation, DataAnnotations, plain code) — what matters is that the class implements the Minded validator interface so the decorator can resolve and invoke it.
- Reuse entity validators across multiple command validators via constructor injection.
- Use `Severity.Error` for hard failures, `Severity.Warning` for advisory checks, `Severity.Info` for informational outcomes.
- Return early after a null guard failure to avoid null-reference cascades.
- Never access the database or call `IMediator` from within a validator.

---

## DI Registration

```csharp
services.AddMinded(
    configuration,
    assembly => assembly.FullName!.StartsWith("YourApp"),
    builder =>
    {
        builder.AddMediator();

        // Command pipeline — first registered = innermost (runs last, closest to handler)
        builder.AddCommandValidationDecorator()   // innermost: validates right before handler
               .AddCommandRetryDecorator()        // retries on transient failures
               .AddCommandLoggingDecorator()      // logs each attempt
               .AddCommandTransactionDecorator()  // wraps in DB transaction (omit if unused)
               .AddCommandExceptionDecorator()    // outermost: catches all unhandled exceptions
               .AddCommandHandlers();

        // Query pipeline — first registered = innermost
        builder.AddQueryValidationDecorator()
               .AddQueryMemoryCacheDecorator()    // omit if no [MemoryCache] queries
               .AddQueryRetryDecorator()          // omit if no [RetryQuery] queries
               .AddQueryLoggingDecorator()
               .AddQueryExceptionDecorator()      // outermost
               .AddQueryHandlers();

        builder.AddRestMediator();                // only when using IRestMediator
    });
```

> **Order rule**: first registered = innermost (runs last). Last registered = outermost (runs first). `AddCommandHandlers()` / `AddQueryHandlers()` register actual handlers and are always the innermost element regardless of call order.

**Include/exclude decorators deliberately**:
- Always include exception decorators in production.
- Include validation decorators only when you have validators.
- Include retry only when commands/queries use `[RetryCommand]` / `[RetryQuery]`.
- Include memory cache only when queries use `[MemoryCache]`.
- Do not remove a decorator globally to exempt one command — use opt-in attributes instead.

---

## REST Controllers

Use `IRestMediator` (from `Minded.Extensions.WebApi`). Controllers inject **only** `IRestMediator` — no handlers, no repositories, no direct `IMediator`.

> **Read-only OData controllers** are the exception: they inject the `DbContext` directly and expose `IQueryable` for OData query composition. Do **not** call `IMediator`/`IRestMediator` from these controllers.

```csharp
[ApiController]
[Route("api/[controller]")]
public class EntityController : ControllerBase
{
    private readonly IRestMediator _restMediator;

    public EntityController(IRestMediator restMediator) => _restMediator = restMediator;

    [HttpGet]
    public async Task<IActionResult> Get(
        ODataQueryOptions<Entity> queryOptions,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEntitiesQuery();
        query.ApplyODataQueryOptions(queryOptions);
        return await _restMediator.ProcessRestQueryAsync(RestOperation.GetMany, query, cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
        => await _restMediator.ProcessRestQueryAsync(
               RestOperation.GetSingle, new GetEntityByIdQuery(id), cancellationToken);

    [HttpPost]
    public async Task<IActionResult> Post(
        [FromBody] Entity entity, CancellationToken cancellationToken = default)
        => await _restMediator.ProcessRestCommandAsync(
               RestOperation.CreateWithContent, new CreateEntityCommand(entity), cancellationToken);

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(
        int id, [FromBody] Entity entity, CancellationToken cancellationToken = default)
        => await _restMediator.ProcessRestCommandAsync(
               RestOperation.UpdateWithContent, new UpdateEntityCommand(id, entity), cancellationToken);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        => await _restMediator.ProcessRestCommandAsync(
               RestOperation.Delete, new DeleteEntityCommand(id), cancellationToken);
}
```

| HTTP verb | `RestOperation` |
|-----------|----------------|
| GET (list) | `RestOperation.GetMany` |
| GET (single) | `RestOperation.GetSingle` |
| POST | `RestOperation.CreateWithContent` |
| PUT | `RestOperation.UpdateWithContent` |
| PATCH | `RestOperation.PatchWithContent` |
| DELETE | `RestOperation.Delete` |

---

## Decorator Attribute Quick Reference

| Attribute / Interface | Package | When to use |
|----------------------|---------|------------|
| `[ValidateCommand]` | Validation | Input validation needed |
| `[ValidateQuery]` | Validation | Query parameter validation needed |
| `[RetryCommand]` | Retry | Idempotent command; transient failure risk (external API, DB deadlock) |
| `[RetryCommand(n, ms1, ms2...)]` | Retry | Custom retry count and backoff delays |
| `[RetryQuery]` | Retry | Query with transient failure risk |
| `[MemoryCache(ExpirationInSeconds = N)]` | Caching.Memory | Frequently-read, relatively stable data |
| `[TransactionalCommand]` | Transaction | Command modifying multiple entities/tables atomically |
| `[RequirePermissions("permission")]` | Authorization | Permission-gated operation |
| `ILoggable` | Logging | Structured logging; provide `LoggingTemplate` + `LoggingProperties` |
| `ITraceable` | Framework | Distributed trace propagation |
| `IGenerateCacheKey` | Caching.Memory | **Required** alongside `[MemoryCache]` |

---

## Testing

### Validator unit tests

```csharp
[TestClass]
public class CreateEntityCommandValidatorTest
{
    private CreateEntityCommandValidator _sut;

    [TestInitialize]
    public void TestInitialize()
        => _sut = new CreateEntityCommandValidator(new EntityValidator());

    [TestMethod]
    public async Task Validation_succeeds_when_entity_is_valid()
    {
        var result = await _sut.ValidateAsync(new CreateEntityCommand(new Entity { Name = "Valid" }));
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validation_fails_when_name_is_empty()
    {
        var result = await _sut.ValidateAsync(new CreateEntityCommand(new Entity { Name = "" }));
        result.IsValid.Should().BeFalse();
        result.OutcomeEntries.Should().ContainSingle(e => e.Key == nameof(Entity.Name));
    }
}
```

### Handler unit tests

```csharp
[TestClass]
public class CreateEntityCommandHandlerTest
{
    private Mock<IEntityRepository> _repoMock;
    private CreateEntityCommandHandler _sut;

    [TestInitialize]
    public void TestInitialize()
    {
        _repoMock = new Mock<IEntityRepository>();
        _sut = new CreateEntityCommandHandler(_repoMock.Object);
    }

    [TestMethod]
    public async Task HandleAsync_creates_entity_and_returns_success()
    {
        var entity = new Entity { Name = "Test" };
        _repoMock.Setup(r => r.CreateAsync(entity, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(entity);

        var result = await _sut.HandleAsync(new CreateEntityCommand(entity));

        result.Successful.Should().BeTrue();
        result.Result.Should().Be(entity);
    }
}
```

---

## Common Pitfalls

| Anti-pattern | Correct approach |
|-------------|-----------------|
| Validation logic inside a handler | Move to `ICommandValidator<T>`, decorate with `[ValidateCommand]` |
| `[MemoryCache]` without `IGenerateCacheKey` | Always implement `IGenerateCacheKey` on the same query class |
| `GetCacheKey()` missing some query properties | Stale cache — include ALL discriminating properties in the key |
| `[RetryCommand]` on non-idempotent operations | Only apply to operations safe to re-run |
| Calling `IMediator` inside a standard CRUD handler | Keep handlers thin; orchestrate at a higher layer |
| Registering exception decorator before others | Exception must be last registered (outermost) to catch all errors |
| Removing a global decorator to skip one command | Use opt-in attributes instead; never remove global decorators |
| `.Result` / `.Wait()` in handlers | Always use `async/await` throughout the handler chain |
