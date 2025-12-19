---
type: "always_apply"
---

# Minded Framework Development Guidelines

Guidelines for developing applications using the Minded Framework with CQRS pattern, REST API, and decorators.

## Core Principles

- Always ask clarifying questions when requirements are unclear
- Follow Single Responsibility Principle - one handler per command/query
- Strict separation: Commands change state, Queries retrieve data
- Commands can use other commands and queries; Queries can only use other queries

## Required Packages

**Framework Core:**
- `Minded.Framework.CQRS` and `Minded.Framework.CQRS.Abstractions`
- `Minded.Framework.Mediator` and `Minded.Framework.Mediator.Abstractions`

**Common Extensions:**
- `Minded.Extensions.WebApi` - RestMediator for REST APIs
- `Minded.Extensions.Validation` - To isolate command and queries validation logic into a dedicated validator class triggered by the framework when the Validate attribute is used on commands or queries
- `Minded.Extensions.Logging` - Structured logging decorator
- `Minded.Extensions.Exception` - Exception handling
- `Minded.Extensions.Retry` - Retry logic for transient failures
- `Minded.Extensions.Transaction` - Transaction management
- `Minded.Extensions.Caching.Memory` - Add memory cache to queries when needed

## Project Structure

```
MyApp.Api/                        # Web API
  Controllers/                    # REST controllers
  ReadOnlyControllers/            # Read only OData controllers specific to serve data to UI 
MyApp.Application.[Domain]/       # Business logic per domain
  Commands/                       # Commands, command handlers and command validators
  Queries/                        # Queries, query handlers and query validators
Domain.Entity/                    # Entities
Infrastructure.Database.Context/  # DbContext
Tests/                            # Tests
```

## REST API Controllers

### Standard API Controllers (`/api/[controller]`)

- Use for domain-driven RESTful endpoints
- Use RestMediator with RestOperation (GetSingle, GetMany, CreateWithContent, UpdateWithContent, Delete)
- Support OData queries via ODataQueryOptions
- Can be reused for UI queries if performance is adequate and limited complexity
- Always include CancellationToken parameter in asyc actions
- Always make sure all security best practices are in place to avoid common attacks

**Example:**
```csharp
[Route("api/[controller]")]
public class CategoryController : Controller
{
    private readonly IRestMediator _restMediator;

    [HttpGet]
    public async Task<IActionResult> Get(
        ODataQueryOptions<Category> queryOptions, 
        CancellationToken cancellationToken = default)
    {
        var query = new GetCategoriesQuery();
        query.ApplyODataQueryOptions(queryOptions);
        return await _restMediator.ProcessRestQueryAsync(
            RestOperation.GetMany, query, cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> Post(
        [FromBody] Category category, 
        CancellationToken cancellationToken = default)
    {
        return await _restMediator.ProcessRestCommandAsync(
            RestOperation.CreateWithContent, 
            new CreateCategoryCommand(category), 
            cancellationToken);
    }
}
```

### Dedicated UI Controllers (Use Sparingly)

Create ONLY when existing `/api/` endpoints might cause performance issues:

**When to Create:**
- Need specific data projections not in domain API
- Multiple API round-trips can be combined into one
- Need specialized optimizations (caching, denormalization, aggregations)

**When to Reuse API:**
- Existing endpoints provide the needed data
- Performance is adequate
- Domain model matches UI needs

**Guidelines:**
- Use route pattern: `/api/ui/[controller]`
- Read-only operations (GET only)
- Use `.AsNoTracking()` for EF queries or better a dedicated read only DbContext
- Document WHY dedicated endpoint was created in XML comments
- Create new DB View if complex mapping is required and a View can help optimise the performance

## Commands (State Changes)

**Naming:** `{Verb}{Noun}Command` (e.g., `CreateCategoryCommand`, `UpdateUserCommand`)

**Structure:**
```csharp
[ValidateCommand]
[RetryCommand]  // For transient failures
[TransactionalCommand]  // For multi-entity operations
public class CreateCategoryCommand : ICommand<Category>, ILoggable
{
    public Category Category { get; set; }
    public Guid TraceId { get; } = Guid.NewGuid();
    
    public string LoggingTemplate => "CategoryName {CategoryName}";
    public string[] LoggingProperties => new[] { "Category.Name" };
}
```

**Rules:**
- Include TraceId for correlation
- Implement ILoggable for structured logging
- Use decorator: `[ValidateCommand]` to encapsulate validation Logic
- Use decorator: `[RetryCommand]` for commands which might be subject to failures, like these consuming external services
- Use decorator: `[TransactionalCommand]` when a complex command handler requires the executon of multiple sub commands with multiple db updates
- Return the modified entity or void
- Should be immutable after construction

## Command Handlers

**Naming:** `{CommandName}Handler`

**Structure:**
```csharp
public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, Category>
{
    private readonly IMindedExampleContext _context;

    public async Task<ICommandResponse<Category>> HandleAsync(
        CreateCategoryCommand command, 
        CancellationToken cancellationToken = default)
    {
        await _context.Categories.AddAsync(command.Category, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new CommandResponse<Category>(true, command.Category);
    }
}
```

**Rules:**
- One handler per command (Single Responsibility)
- Can invoke other command handlers or query handlers via mediator
- Always use CancellationToken properly
- Use async/await for all I/O
- Return ICommandResponse<T> with Successful flag
- Always pass TraceId for correlation

## Queries (Data Retrieval)

**Naming:** `Get{Noun}[Suffix]Query` (e.g., `GetCategoryByIdQuery`, `GetUsersQuery`)

**Structure:**
```csharp
[ValidateQuery]
public class GetCategoriesQuery : 
    IQuery<IQueryResponse<IEnumerable<Category>>>, 
    ICanCount, 
    ICanTop, 
    ICanSkip, 
    ICanExpand, 
    ICanOrderBy, 
    ICanFilterExpression<Category>, 
    ILoggable
{
    public bool Count { get; set; }
    public int? Top { get; set; }
    public int? Skip { get; set; }
    public string[] Expand { get; set; }
    public IList<OrderDescriptor> OrderBy { get; set; }
    public Expression<Func<Category, bool>> Filter { get; set; }
    public Guid TraceId { get; } = Guid.NewGuid();
    
    public string LoggingTemplate => "Top: {Top} - Skip: {Skip}";
    public string[] LoggingProperties => new[] { nameof(Top), nameof(Skip) };
}
```

**Rules:**
- Implement appropriate traits: `ICanFilter`, `ICanOrderBy`, `ICanTop`, `ICanSkip`, `ICanCount`, `ICanExpand`
- Include TraceId for correlation
- Implement ILoggable
- Should be immutable
- Never modify state

## Query Handlers

**Structure:**
```csharp
public class GetCategoriesQueryHandler : 
    IQueryHandler<GetCategoriesQuery, IQueryResponse<IEnumerable<Category>>>
{
    private readonly IMindedExampleContext _context;

    public async Task<IQueryResponse<IEnumerable<Category>>> HandleAsync(
        GetCategoriesQuery query, 
        CancellationToken cancellationToken = default)
    {
        List<Category> result = await query
            .ApplyQueryTo(_context.Categories.AsQueryable())
            .ToListAsync(cancellationToken);
        
        return new QueryResponse<IEnumerable<Category>>(result);
    }
}
```

**Rules:**
- One handler per query
- Can invoke other query handlers (composition)
- Should NOT invoke command handlers
- Use `.AsNoTracking()` for read-only when appropriate
- Apply query traits using `.ApplyQueryTo()`
- Navigation properties NOT loaded by default - require explicit `$expand`

## Validation

**Structure:**
```csharp
public class CreateCategoryCommandValidator : ICommandValidator<CreateCategoryCommand>
{
    private readonly IValidator<Category> _categoryValidator;

    public async Task<IValidationResult> ValidateAsync(CreateCategoryCommand command)
    {
        var validationResult = new ValidationResult();

        if (command.Category == null)
        {
            validationResult.OutcomeEntries.Add(
                new OutcomeEntry(
                    nameof(command.Category), 
                    "{0} is mandatory", 
                    GenericErrorCodes.ValidationFailed, 
                    Severity.Error));
            return validationResult;
        }

        var result = await _categoryValidator.ValidateAsync(command.Category);
        return result.Merge(validationResult);
    }
}
```

**Rules:**
- Create separate validators: `ICommandValidator<T>`, `IQueryValidator<T>`, `IValidator<TEntity>`
- Use `[ValidateCommand]` or `[ValidateQuery]` attribute to enable
- Compose validators (reuse entity validators in command validators where applicable)
- Return IValidationResult with OutcomeEntry for each error
- Use GenericErrorCodes or custom error codes
- Mark severity: Error, Warning, or Info

## Decorator Attributes

### Transaction Management

Use `[TransactionalCommand]` when multiple commands/entities must succeed or fail atomically:

```csharp
[ValidateCommand]
[TransactionalCommand(
    IsolationLevel = IsolationLevel.ReadCommitted,
    TimeoutSeconds = 30)]
public class CreateOrderWithItemsCommand : ICommand<Order>
```

**When to Use:**
- Multiple commands modify data together
- Multiple tables must be updated consistently
- Generally not needed for single-entity CRUD

### Retry Logic

Use `[RetryCommand]` or `[RetryQuery]` for operations with transient failures:

```csharp
[ValidateCommand]
[RetryCommand(3, 100, 200, 400)]  // 3 retries with delays
public class CallExternalApiCommand : ICommand<ApiResponse>
```

**When to Use:**
- External API calls
- Network operations
- Database deadlocks
- Transient failures

### Caching

Use memory cache decorator for queries that benefit from caching:

**When to Cache:**
- Frequently accessed data
- Expensive queries (complex joins, aggregations)
- Relatively static reference data

**When NOT to Cache:**
- Rapidly changing transactional data
- User-specific data changing frequently
- Real-time requirements

```csharp
[ValidateQuery]
[CacheQuery(DurationSeconds = 300)]
public class GetCategoriesQuery : IQuery<IQueryResponse<IEnumerable<Category>>>
```

## Startup Configuration

```csharp
services.AddMinded(Configuration, 
    assembly => assembly.Name.StartsWith("Service."), 
    builder =>
{
    builder.AddMediator();
    builder.AddRestMediator();
    
    // Command Pipeline (order matters)
    builder.AddCommandValidationDecorator()
           .AddCommandExceptionDecorator(options => options.Serialize = true)
           .AddCommandRetryDecorator(options =>
           {
               options.DefaultRetryCount = 3;
               options.DefaultDelay1 = 100;
               options.DefaultDelay2 = 200;
               options.DefaultDelay3 = 400;
           })
           .AddCommandLoggingDecorator(options => options.Enabled = true)
           .AddCommandTransactionDecorator()
           .AddCommandHandlers();

    // Query Pipeline
    builder.AddQueryValidationDecorator()
           .AddQueryExceptionDecorator(options => options.Serialize = true)
           .AddQueryRetryDecorator(applyToAllQueries: false)
           .AddQueryLoggingDecorator(options => options.Enabled = true)
           .AddQueryMemoryCacheDecorator()
           .AddQueryHandlers();
});

// MVC with OData
services.AddControllers()
        .AddODataNavigationPropertySerialization()
        .AddOData(options => options
            .Select()
            .Filter()
            .OrderBy()
            .Expand()
            .Count()
            .SetMaxTop(100));
```

## Testing

### Libraries

- **AnonymousData** (https://github.com/norcino/TestingSupportPackages/blob/master/AnonymousData)
  - Use `Any.Int()`, `Any.String()`, `Any.DateTime()` for test data where value doesn't matter
  
- **Object.Builder** (https://github.com/norcino/TestingSupportPackages/tree/master/Builder)
  - Use for complex object construction

**Rules:**
- Use static values ONLY when asserting that specific value
- Use `Any.*()` for properties that must be filled but value doesn't matter

### Unit Tests

```csharp
[TestClass]
public class CategoryControllerTests
{
    private Mock<IRestMediator> _mediatorMock;

    [TestInitialize]
    public void Setup()
    {
        _mediatorMock = new Mock<IRestMediator>();
    }

    [TestMethod]
    public async Task Post_invokes_RestMediator_with_correct_operation()
    {
        var category = new Category { Name = Any.String() };
        var controller = new CategoryController(_mediatorMock.Object);

        await controller.Post(category, default);

        _mediatorMock.Verify(m => m.ProcessRestCommandAsync(
            RestOperation.CreateWithContent,
            It.Is<CreateCategoryCommand>(c => c.Category == category),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

### End-to-End Tests

```csharp
[TestClass]
[DoNotParallelize]
public class CategoryE2ETests : BaseE2ETest
{
    [TestMethod]
    public async Task POST_creates_category_and_returns_201()
    {
        var user = await SeedOne<User>(c => c.Id);
        var category = new Category
        {
            Name = Any.String(),
            UserId = user.Id
        };

        var response = await _sutClient.PostAsJsonAsync("/api/category", category);

        response.Should().HaveHttpStatusCode(HttpStatusCode.Created);
        var created = await response.Content.ReadAsAsync<Category>();
        created.Id.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task GET_supports_OData_filter()
    {
        var user = await SeedOne<User>(c => c.Id);
        await Seed<Category>(c => c.Id, 10, (c, i) =>
        {
            c.Name = $"Category{i}";
            c.UserId = user.Id;
            c.Active = i % 2 == 0;
        });

        var response = await _sutClient.GetAsync("/api/category?$filter=Active eq true");

        response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
        var categories = await response.Content.ReadAsAsync<List<Category>>();
        categories.Should().OnlyContain(c => c.Active);
    }
}
```

**E2E Test Rules:**
- Inherit from BaseE2ETest (provides TestHost and seeding)
- Use `[DoNotParallelize]` to prevent database conflicts
- Support in-memory SQLite and SQL Server
- Use `Seed<T>()` and `SeedOne<T>()` for test data
- Test OData capabilities: $filter, $orderby, $top, $skip, $count, $expand

## Naming Conventions

- **Commands:** `{Verb}{Noun}Command` (CreateCategoryCommand, UpdateUserCommand)
- **Queries:** `Get{Noun}[Suffix]Query` (GetCategoryByIdQuery, GetUsersQuery)
- **Handlers:** `{CommandOrQueryName}Handler` (CreateCategoryCommandHandler)
- **Validators:** `{CommandOrQueryOrEntity}Validator` (CreateCategoryCommandValidator)
- **Controllers:** `{Noun}Controller` (CategoryController)

## Coding Standards

- Always provide XML documentation for controllers, commands, queries, handlers
- Always use async/await for I/O operations
- Always pass and honor CancellationToken
- Use constructor injection, depend on interfaces
- Navigation properties require explicit `$expand` - never load automatically

## Clarification Questions to Ask

When receiving requirements, ask:

- Which domain does this belong to?
- Can existing `/api/` endpoint be used, or is dedicated UI controller needed?
- What validation rules apply?
- Does this involve multiple data modifications requiring `[TransactionalCommand]`?
- Is this data accessed frequently or expensive to load (should it be cached)?
- What are the specific performance requirements?
- Should navigation properties be available for expansion?

## Quick Checklist

- [ ] Determine if existing `/api/` endpoint can be reused
- [ ] Create Command/Query with appropriate attributes
- [ ] Create Handler with Single Responsibility
- [ ] Create Validator if needed
- [ ] Use `[ValidateCommand]` or `[ValidateQuery]`
- [ ] Use `[RetryCommand]` for transient failures
- [ ] Use `[TransactionalCommand]` for multi-entity operations
- [ ] Consider `[CacheQuery]` for frequently accessed data
- [ ] Implement ILoggable for structured logging
- [ ] Ensure navigation properties only load via `$expand`
- [ ] Add XML documentation
- [ ] Create unit tests with AnonymousData