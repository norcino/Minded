---
applyTo: "Example/**/*.cs"
---

# Minded Framework – Backend Clean Architecture Guidelines

These rules apply to all C# code in this repository. They enforce clean architecture, CQRS discipline, and correct use of the Minded framework decorators, testing patterns, and REST mediator conventions.

---

## 1. Project & Folder Structure

Each service lives in its own project (e.g. `Application.Category`, `Application.Transaction`). Inside each service project use exactly these top-level folders:

```
Application.{Feature}/
├── Command/          # ICommand / ICommand<TResult> definitions
├── CommandHandler/   # ICommandHandler<TCommand> / ICommandHandler<TCommand, TResult>
├── Query/            # IQuery<TResult> definitions
├── QueryHandler/     # IQueryHandler<TQuery, TResult>
└── Validator/        # ICommandValidator<T>, IQueryValidator<T, R>, IValidator<TEntity>
```

Namespaces follow the folder: `Application.{Feature}.Command`, `Application.{Feature}.CommandHandler`, etc.

---

## 2. Commands

- Implement `ICommand` (void result) or `ICommand<TResult>` (with result).
- Always include `public Guid TraceId { get; } = Guid.NewGuid();` with an optional constructor override.
- Commands that mutate state and can be safely retried **must** carry `[RetryCommand]`.
- Commands that require input validation **must** carry `[ValidateCommand]`.
- Commands that should be logged **must** implement `ILoggable` and provide `LoggingTemplate` and `LoggingProperties`.

```csharp
[ValidateCommand]
[RetryCommand]                          // omit only for non-idempotent mutations
public class CreateCategoryCommand : ICommand<Data.Entity.Category>, ILoggable
{
    public Data.Entity.Category Category { get; set; }

    public CreateCategoryCommand(Data.Entity.Category category, Guid? traceId = null)
    {
        Category = category;
        TraceId = traceId ?? TraceId;
    }

    public Guid TraceId { get; } = Guid.NewGuid();
    public string LoggingTemplate => "CategoryName {CategoryName}";
    public string[] LoggingProperties => new[] { "Category.Name" };
}
```

**Retry rules:**
- Apply `[RetryCommand]` (uses global defaults) on idempotent write commands (create, update, delete that are safe to re-run).
- Never apply `[RetryCommand]` to commands that are NOT idempotent (e.g. financial debit, send-email).
- Custom retry delays: `[RetryCommand(3, 100, 200, 400)]` — 3 retries at 100 ms, 200 ms, 400 ms.

---

## 3. Queries

- Implement `IQuery<TResult>` where `TResult` is usually `IQueryResponse<IEnumerable<TEntity>>` for lists or `TEntity` for single-item queries.
- Collection queries must implement the relevant OData trait interfaces: `ICanCount`, `ICanTop`, `ICanSkip`, `ICanExpand`, `ICanOrderBy`, `ICanFilterExpression<TEntity>`.
- Queries that require input validation **must** carry `[ValidateQuery]`.
- Queries that should be logged **must** implement `ILoggable`.
- Frequently-read, stable data queries **should** implement `IGenerateCacheKey` and carry `[MemoryCache(ExpirationInSeconds = N)]`.

```csharp
[ValidateQuery]
public class GetCategoriesQuery : IQuery<IQueryResponse<IEnumerable<Category>>>,
    ICanCount, ICanTop, ICanSkip, ICanExpand, ICanOrderBy,
    ICanFilterExpression<Category>, ILoggable
{
    public bool CountOnly { get; set; }
    public bool Count { get; set; }
    public int CountValue { get; set; }
    public int? Top { get; set; }
    public int? Skip { get; set; }
    public string[] Expand { get; set; }
    public IList<OrderDescriptor> OrderBy { get; set; }
    public Expression<Func<Category, bool>> Filter { get; set; }

    public GetCategoriesQuery(Guid? traceId = null) { TraceId = traceId ?? TraceId; }

    public Guid TraceId { get; } = Guid.NewGuid();
    public string LoggingTemplate => "Count: {Count} - Top: {Top} - Skip: {Skip}";
    public string[] LoggingProperties => new[] { nameof(Count), nameof(Top), nameof(Skip) };
}
```

**Cache rules:**
- Both `[MemoryCache(ExpirationInSeconds = N)]` attribute AND `IGenerateCacheKey` interface are required together; use neither alone.
- `GetCacheKey()` must return a string that uniquely identifies the query result (include all discriminating properties).
- Use `SlidingExpiration` for user-session-scoped data; use absolute `ExpirationInSeconds` for reference/lookup data.

---

## 4. Command Handlers

- A command handler does exactly one thing: execute the business action.
- Inject only what is directly needed (e.g. `DbContext`, domain services).
- Do **not** perform validation inside a handler — validation belongs in a validator.
- Do **not** invoke another handler class directly. When a handler must trigger other commands/queries (orchestration), dispatch them through `IMediator`; standard CRUD handlers should not need `IMediator`.

```csharp
public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, Data.Entity.Category>
{
    private readonly IMindedExampleContext _context;

    public CreateCategoryCommandHandler(IMindedExampleContext context) => _context = context;

    public async Task<ICommandResponse<Data.Entity.Category>> HandleAsync(
        CreateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        await _context.Categories.AddAsync(command.Category, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return CommandResponse<Data.Entity.Category>.Success(command.Category);
    }
}
```

---

## 5. Query Handlers

- Query handlers **must not** modify state.
- Build queries using `IQueryable` and apply OData traits from the query object (Top, Skip, Filter, OrderBy, etc.) before materializing.

---

## 6. Validators

Every command or query decorated with `[ValidateCommand]` / `[ValidateQuery]` **must** have a corresponding validator in the `Validator/` folder.

### Command validator
```csharp
public class CreateCategoryCommandValidator : ICommandValidator<CreateCategoryCommand>
{
    private readonly IValidator<Data.Entity.Category> _categoryValidator;

    public CreateCategoryCommandValidator(IValidator<Data.Entity.Category> categoryValidator)
        => _categoryValidator = categoryValidator;

    public async Task<IValidationResult> ValidateAsync(CreateCategoryCommand command)
    {
        var result = new ValidationResult();

        if (command.Category == null)
        {
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(command.Category), "{0} is mandatory",
                attemptedValue: null, Severity.Error, GenericErrorCodes.ValidationFailed));
            return result;
        }

        return (await _categoryValidator.ValidateAsync(command.Category)).Merge(result);
    }
}
```

### Entity validator
- Implement `IValidator<TEntity>` separately from command validators.
- Reuse entity validators across multiple command validators.
- Use `Severity.Error` for hard failures, `Severity.Warning` for advisory checks, `Severity.Info` for informational outcomes.
- Return early when a null guard fails to avoid null-reference cascades.

---

## 7. REST Mediator & Controllers

- Controllers inject `IRestMediator` only — no handlers, no repositories.
- For OData-enabled collection endpoints inject `ODataQueryOptions<TEntity>` and call `query.ApplyODataQueryOptions(queryOptions)`.
- Always accept `CancellationToken cancellationToken = default` in controller actions.
- Inject `IRestMediator` only — never `IMediator` or any handler directly.
- Do not inject `IMindedExampleContext` (or any DbContext) in controllers — except read-only OData controllers, which inject the DbContext directly and expose `IQueryable` (no `IMediator`/`IRestMediator` calls in those controllers).
- Do not execute LINQ/data access inside controllers.
- Controllers are transport adapters only: map HTTP request to command/query and delegate to `IRestMediator`.
- Non-trivial authorization checks must be implemented with ASP.NET authorization policies, decorators, or handlers, not with inline controller business logic.
- Map HTTP verbs to `RestOperation` values:

| HTTP method        | RestOperation              |
|--------------------|----------------------------|
| `GET` (collection) | `RestOperation.GetMany`    |
| `GET` (single)     | `RestOperation.GetSingle`  |
| `POST`             | `RestOperation.CreateWithContent` |
| `PUT`              | `RestOperation.UpdateWithContent` |
| `PATCH`            | `RestOperation.PatchWithContent`  |
| `DELETE`           | `RestOperation.Delete`     |

```csharp
[Route("api/[controller]")]
public class CategoryController : Controller
{
    private readonly IRestMediator _restMediator;
    public CategoryController(IRestMediator restMediator) => _restMediator = restMediator;

    [HttpGet]
    public async Task<IActionResult> Get(
        ODataQueryOptions<Category> queryOptions,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCategoriesQuery();
        query.ApplyODataQueryOptions(queryOptions);
        return await _restMediator.ProcessRestQueryAsync(RestOperation.GetMany, query, cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> Post(
        [FromBody] Category category,
        CancellationToken cancellationToken = default)
        => await _restMediator.ProcessRestCommandAsync(
               RestOperation.CreateWithContent,
               new CreateCategoryCommand(category), cancellationToken);
}
```

### Non-negotiable API architecture gate

Before considering backend work complete, verify all these conditions:

1. Every endpoint with business behavior has a dedicated command/query.
2. Business logic lives in handlers, not in controllers.
3. Controller actions contain only: input mapping, `RestOperation` selection, mediator delegation.
4. No direct persistence dependencies (`IMindedExampleContext`, EF query APIs) in controllers (read-only OData controllers excepted).
5. Any controller-level exception handling must be transport-only and not business orchestration.

---

## 8. Decorator Registration (DI)

Register decorators in Program.cs / Startup.cs via `services.AddMinded(configuration, mindedBuilderConfiguration: builder => { ... })`. **Decorators are registered from innermost to outermost**: the first decorator registered is innermost (runs last, right before the handler); the last registered is outermost (runs first, earliest interception). `AddCommandHandlers()` / `AddQueryHandlers()` register the actual handlers and are always innermost regardless of call order.

```csharp
services.AddMinded(configuration, mindedBuilderConfiguration: builder =>
{
    builder.AddCommandValidationDecorator()   // innermost decorator: validates right before handler
           .AddCommandRetryDecorator()        // wraps validation + handler; retries on exception
           .AddCommandLoggingDecorator()      // logs each attempt including retries
           .AddCommandExceptionDecorator()    // outermost: catches all unhandled exceptions
           .AddCommandHandlers();

    builder.AddQueryValidationDecorator()
           .AddQueryMemoryCacheDecorator()    // short-circuits before handler if cached
           .AddQueryLoggingDecorator()
           .AddQueryExceptionDecorator()      // outermost
           .AddQueryHandlers();

    builder.AddRestMediator();
});
```

Do **not** skip a decorator layer globally to exclude one command/query — use opt-in attributes (`[ValidateCommand]`, `[RetryCommand]`, `[MemoryCache]`) to control per-command/query behaviour.

---

## 9. Test Coverage

Three test tiers are required for every feature. Test projects live in `Example/Tests/`:

### 9.1 Unit tests — `Application.{Feature}.Tests`
- Test **validators** in isolation.
- Use MSTest (`[TestClass]`, `[TestMethod]`) with FluentAssertions for assertions.
- Instantiate the validator directly (no DI container).
- Use the `Builder<T>` helper to construct entities.
- Cover: valid input succeeds, each required field missing fails with the correct `OutcomeEntry`.

```csharp
[TestClass]
public class CreateCategoryCommandValidatorTest
{
    private CreateCategoryCommandValidator _sut;

    [TestInitialize]
    public void TestInitialize()
        => _sut = new CreateCategoryCommandValidator(new CategoryValidator());

    [TestMethod]
    public async Task Validation_succeed_when_category_is_valid_for_creation()
    {
        var category = Builder<Category>.New().Build(e => { e.Name = "Valid"; e.Id = 0; });
        var result = await _sut.ValidateAsync(new CreateCategoryCommand(category));
        result.IsValid.Should().BeTrue();
    }
}
```

### 9.2 Integration tests — `Application.{Feature}.IntegrationTests`
- Test **command/query handlers** end-to-end against a real (in-memory SQLite) database.
- Extend `BaseServiceIntegrationTest` which sets up the EF context and mediator.
- Assert on the database state after the command executes (use `Context.{DbSet}.SingleAsync(...)`).
- Use FluentAssertions `.Should().BeEquivalentTo(...)` for entity comparisons.

### 9.3 E2E tests — `Application.Api.E2ETests`
- Test HTTP endpoints through the full ASP.NET pipeline.
- Extend `BaseE2ETest` which spins up the `WebApplicationFactory`.
- Use `_sutClient` (HttpClient) to make real HTTP calls.
- Seed data via `SeedOne<T>()` / `Seed<T>()` helpers before each test.
- Assert HTTP status codes with `response.Should().HaveHttpStatusCode(HttpStatusCode.OK)`.
- Deserialise response bodies with `response.Content.ReadAsAsync<T>()`.
- Annotate tests requiring sequential execution with `[DoNotParallelize]`.

```csharp
[DoNotParallelize]
[TestClass]
public class CategoryE2ETests : BaseE2ETest
{
    [TestMethod]
    public async Task POST_creates_a_category_and_returns_201()
    {
        var user = await SeedOne<User>(c => c.Id);
        var category = Builder<Category>.New().Build(c => { c.Name = "NewCat"; c.UserId = user.Id; });

        var response = await _sutClient.PostAsJsonAsync("/api/category", category);

        response.Should().HaveHttpStatusCode(HttpStatusCode.Created);
        var created = await response.Content.ReadAsAsync<Category>();
        created.Name.Should().Be(category.Name);
    }
}
```

---

## 10. Clean Architecture Boundaries

- **Controllers** depend on: `IRestMediator`, query/command types, entity types.
- **Command/Query types** depend on: framework interfaces, entity types, decorator attributes.
- **Handlers** depend on: `DbContext`/repositories, domain services — **never** on controllers or mediator.
- **Validators** depend on: other `IValidator<T>` — **never** on handlers or DbContext directly.
- **No circular project references** are permitted.
- Shared entities live in `Data.Entity`; shared DB context interfaces live in `Data.Context`.
- Cross-cutting concerns (logging, retry, caching, validation, exception handling) are handled exclusively through the decorator pipeline — **not** inline in handlers.
