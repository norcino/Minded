[![Build status](https://dev.azure.com/norcino/Minded/_apis/build/status/GitHub%20Minded)](https://dev.azure.com/norcino/Minded/_build/latest?definitionId=1)
[![NuGet](https://img.shields.io/nuget/v/Minded.Framework.Mediator.svg)](https://www.nuget.org/packages/Minded.Framework.Mediator/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

# Minded Framework

**Clean Architecture Made Simple** - A comprehensive .NET framework that implements Mediator, CQRS, and Decorator patterns to help you build maintainable, testable, and scalable applications.

---

## Table of Contents

- [Introduction](#introduction)
- [Why Minded?](#why-minded)
- [Core Concepts](#core-concepts)
- [Getting Started](#getting-started)
- [For Engineers Adopting Minded](#for-engineers-adopting-minded)
- [For Engineers Extending Minded](#for-engineers-extending-minded)
- [For Contributors](#for-contributors)
- [Example Application](#example-application)
- [Available Packages](#available-packages)
- [Documentation](#documentation)
- [License](#license)

---

## Introduction

The **Minded Framework** derives its name from "**M**ed**I**ator comma**ND** qu**E**ry **D**ecorator" - the three foundational patterns that power this framework. Minded helps you implement clean architecture principles by providing a structured, opinionated approach to building applications using:

- **Mediator Pattern** - Decouples business logic from infrastructure
- **CQRS (Command Query Responsibility Segregation)** - Separates read and write operations
- **Decorator Pattern** - Adds cross-cutting concerns (validation, logging, caching, etc.) without modifying core logic

The framework enforces best practices around **Encapsulation**, **Reusability**, **Maintainability**, **Extensibility**, and **Low Coupling**, making it easier to build robust, production-ready applications.

> **Note**: Minded is not a silver bullet, but it provides the scaffolding and structure to help teams write cleaner, more maintainable code.

---

## Why Minded?

### Clean Architecture Benefits

Minded Framework helps you achieve clean architecture by:

1. **Separation of Concerns** - Business logic lives in Commands and Queries, completely isolated from infrastructure concerns
2. **Dependency Inversion** - Your domain logic depends on abstractions, not concrete implementations
3. **Testability** - Handlers are simple, focused classes that are easy to unit test
4. **Maintainability** - Changes to cross-cutting concerns (logging, validation) don't require modifying business logic
5. **Scalability** - The decorator pattern allows you to add new behaviors without changing existing code
6. **Consistency** - All operations follow the same pattern, making the codebase predictable and easier to navigate

### Key Advantages

- **Reduced Boilerplate** - No repetitive try/catch blocks or manual validation in controllers
- **Built-in CQRS** - Commands and Queries flow naturally through handlers
- **Automatic Dependency Injection** - Handlers and decorators are auto-registered
- **Centralized Cross-Cutting Concerns** - Validation, logging, caching, exception handling via decorators
- **RESTful API Support** - RestMediator automatically maps operations to HTTP responses
- **Production-Ready** - Includes cancellation token support, proper error handling, and monitoring-friendly logging

---

## Core Concepts

### Mediator Pattern

The **Mediator** acts as a central dispatcher that routes Commands and Queries to their appropriate handlers. This eliminates direct dependencies between your controllers/services and your business logic.

**Benefits:**
- **Loose Coupling** - Controllers don't know about handlers; they only know about the Mediator
- **Single Responsibility** - Each handler does one thing and does it well
- **Easier Testing** - Mock the Mediator to test controllers; test handlers in isolation
- **Simplified Communication** - All requests flow through a single, predictable pipeline

### CQRS (Command Query Responsibility Segregation)

Minded enforces a clear separation between operations that **change state** (Commands) and operations that **read data** (Queries).

**Commands:**
- Represent actions that modify state (Create, Update, Delete)
- Return success/failure status and optionally a result
- Can be validated, logged, and wrapped in transactions

**Queries:**
- Represent read operations (Get, List, Search)
- Return data without side effects
- Can be cached, validated, and logged

**Benefits:**
- **Clarity** - It's immediately obvious whether an operation changes data
- **Optimization** - Queries can be cached; Commands can be transactional
- **Scalability** - Read and write models can evolve independently

### Decorator Pattern

Decorators wrap handlers in layers of cross-cutting concerns, creating an "onion" architecture where each layer has a single responsibility.

**Execution Flow:**
```
Request → Validation → Exception Handling → Logging → Caching → Transaction → Handler → Response
```

**Benefits:**
- **Open/Closed Principle** - Add new behaviors without modifying existing code
- **Composability** - Mix and match decorators as needed
- **Reusability** - Write a decorator once, apply it to all handlers
- **Maintainability** - Change logging/validation logic in one place

---

## Getting Started

### Requirements

- **.NET Standard 2.0+** or **.NET Core 2.0+** or **.NET 6/8+**
- Compatible with ASP.NET Core applications
- Works with Entity Framework Core for data access

### Installation

Install the core Minded packages via NuGet:

```bash
# Core framework
dotnet add package Minded.Framework.Mediator
dotnet add package Minded.Framework.CQRS.Abstractions

# Extensions (choose what you need)
dotnet add package Minded.Extensions.WebApi          # For REST APIs
dotnet add package Minded.Extensions.Validation      # For validation
dotnet add package Minded.Extensions.Logging         # For logging
dotnet add package Minded.Extensions.Exception       # For exception handling
dotnet add package Minded.Extensions.Caching.Memory  # For caching
dotnet add package Minded.Extensions.OData           # For OData support
```

### Quick Start

#### 1. Configure Minded in `Program.cs` or `Startup.cs`

```csharp
services.AddMinded(assembly => assembly.Name.StartsWith("YourApp."), builder =>
{
    // Add Mediator
    builder.AddMediator();
    builder.AddRestMediator(); // For REST APIs

    // Configure Command pipeline (order matters!)
    builder.AddCommandValidationDecorator()    // 1. Validate first
           .AddCommandExceptionDecorator()     // 2. Handle exceptions
           .AddCommandLoggingDecorator()       // 3. Log execution
           .AddCommandHandlers();              // 4. Execute handler

    // Configure Query pipeline
    builder.AddQueryValidationDecorator()
           .AddQueryExceptionDecorator()
           .AddQueryLoggingDecorator()
           .AddQueryMemoryCacheDecorator()     // Optional: Add caching
           .AddQueryHandlers();
});
```

#### 2. Create a Command

```csharp
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

[ValidateCommand]
public class CreateCategoryCommand : ICommand<Category>
{
    public Category Category { get; set; }

    public CreateCategoryCommand(Category category)
    {
        Category = category;
    }
}
```

#### 3. Create a Command Handler

```csharp
using Minded.Framework.CQRS.Command;

public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, Category>
{
    private readonly IDbContext _context;

    public CreateCategoryCommandHandler(IDbContext context)
    {
        _context = context;
    }

    public async Task<ICommandResponse<Category>> HandleAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        await _context.Categories.AddAsync(command.Category, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new CommandResponse<Category>(command.Category)
        {
            Successful = true
        };
    }
}
```

#### 4. Create a Validator (Optional)

```csharp
using Minded.Extensions.Validation;

public class CreateCategoryCommandValidator : ICommandValidator<CreateCategoryCommand>
{
    public async Task<IValidationResult> ValidateAsync(CreateCategoryCommand command)
    {
        var result = new ValidationResult();

        if (command.Category == null)
        {
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(command.Category),
                "{0} is mandatory"));
        }

        if (string.IsNullOrWhiteSpace(command.Category?.Name))
        {
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(command.Category.Name),
                "{0} is mandatory"));
        }

        return result;
    }
}
```

#### 5. Use in a Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly IRestMediator _restMediator;

    public CategoryController(IRestMediator restMediator)
    {
        _restMediator = restMediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Category category)
    {
        return await _restMediator.ProcessRestCommandAsync(
            RestOperation.CreateWithContent,
            new CreateCategoryCommand(category));
    }
}
```

That's it! The framework automatically:
- Validates the command
- Handles exceptions
- Logs execution
- Returns appropriate HTTP status codes
- Supports cancellation tokens

---
## For Engineers Adopting Minded

This section is for engineers who want to use Minded in their applications.

### Understanding the Request Flow

When a request comes into your application, it flows through the Minded pipeline like this:

```
Controller/API Endpoint
    ↓
RestMediator (or Mediator)
    ↓
Validation Decorator ──→ [Fails? Return validation errors]
    ↓
Exception Decorator ──→ [Exception? Log and handle]
    ↓
Logging Decorator ──→ [Log request/response]
    ↓
Caching Decorator (Queries only) ──→ [Cached? Return cached result]
    ↓
Transaction Decorator (Commands only) ──→ [Wrap in transaction]
    ↓
Handler (Your Business Logic)
    ↓
Response flows back through decorators
    ↓
HTTP Response (via RestMediator)
```

### Available Decorators

#### Exception Decorator

**Purpose**: Centralized exception handling and logging

**Usage**:
```csharp
builder.AddCommandExceptionDecorator();
builder.AddQueryExceptionDecorator();
```

**Features**:
- Catches all exceptions from handlers
- Logs exceptions with full context
- Distinguishes between `OperationCanceledException` (logged as Information) and real errors (logged as Error)
- Wraps exceptions in `CommandHandlerException` or `QueryHandlerException`

**See**: [Exception Handling Documentation](Extensions/Minded.Extensions.Exception/README_CancellationHandling.md)

#### Validation Decorator

**Purpose**: Validate commands and queries before execution

**Usage**:
```csharp
builder.AddCommandValidationDecorator();
builder.AddQueryValidationDecorator();
```

**How it works**:
1. Decorate your command/query with `[ValidateCommand]` or `[ValidateQuery]`
2. Create a validator implementing `ICommandValidator<T>` or `IQueryValidator<T, TResult>`
3. The decorator automatically invokes the validator
4. If validation fails, the pipeline stops and returns validation errors

**Example**:
```csharp
[ValidateCommand]
public class CreateCategoryCommand : ICommand<Category>
{
    public Category Category { get; set; }
}

public class CreateCategoryCommandValidator : ICommandValidator<CreateCategoryCommand>
{
    public async Task<IValidationResult> ValidateAsync(CreateCategoryCommand command)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(command.Category?.Name))
        {
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(command.Category.Name),
                "Category name is required"));
        }

        return result;
    }
}
```

#### Retry Decorator

**Purpose**: Automatically retry commands and queries that fail due to transient errors

**Usage**:
```csharp
// For commands - with default retry configuration
builder.AddCommandRetryDecorator(options =>
{
    options.DefaultRetryCount = 3;
    options.DefaultDelay1 = 100;  // First retry after 100ms
    options.DefaultDelay2 = 200;  // Second retry after 200ms
    options.DefaultDelay3 = 400;  // Third retry after 400ms
});

// For queries - with ApplyToAllQueries option
builder.AddQueryRetryDecorator(applyToAllQueries: false, configureOptions: options =>
{
    options.DefaultRetryCount = 2;
    options.DefaultDelay1 = 50;
    options.DefaultDelay2 = 100;
});
```

**How it works**:
1. Decorate your command/query with `[RetryCommand]` or `[RetryQuery]`
2. Specify retry count and delay intervals (up to 5 different delays)
3. If the handler throws an exception, the decorator retries with the specified delays
4. If all retries are exhausted, the exception is propagated

**Command Example**:
```csharp
// Retry up to 3 times with increasing delays (100ms, 200ms, 400ms)
[RetryCommand(3, 100, 200, 400)]
public class CreateCategoryCommand : ICommand<Category>
{
    public Category Category { get; set; }
}

// Use default retry settings from configuration
[RetryCommand]
public class UpdateCategoryCommand : ICommand<Category>
{
    public Category Category { get; set; }
}
```

**Query Example**:
```csharp
// Retry up to 2 times with 50ms delay between attempts
[RetryQuery(2, 50)]
public class GetCategoryByIdQuery : IQuery<Category>
{
    public int CategoryId { get; set; }
}
```

**Features**:
- **Configurable retry count**: Specify how many times to retry (1-5 retries)
- **Custom delay intervals**: Define up to 5 different delay values for exponential backoff
- **Fallback delays**: If fewer delays than retries, the last delay value is reused
- **Default configuration**: Set defaults via dependency injection for commands/queries without explicit values
- **ApplyToAllQueries**: Option to apply retry logic to all queries, even without the attribute
- **Detailed logging**: Logs each retry attempt with exception details

**Best Practices**:
- **Only use retry for idempotent operations** - Operations that can be safely repeated without side effects
- **Use for transient failures** - Network timeouts, temporary database unavailability, rate limiting
- **Implement exponential backoff** - Increase delays between retries (e.g., 100ms, 200ms, 400ms)
- **Limit retry count** - Typically 2-3 retries is sufficient for most scenarios
- **Don't retry validation errors** - Use the Validation Decorator to catch these before execution

**Decorator Order**:
The retry decorator should be registered **after** validation and exception decorators but **before** logging:
```csharp
builder.AddCommandValidationDecorator()
    .AddCommandExceptionDecorator()
    .AddCommandRetryDecorator(options => { /* config */ })
    .AddCommandLoggingDecorator()
    .AddCommandHandlers();
```

#### Logging Decorator

**Purpose**: Log all command and query executions

**Usage**:
```csharp
builder.AddCommandLoggingDecorator();
builder.AddQueryLoggingDecorator();
```

**Features**:
- Logs command/query type and parameters
- Logs execution time
- Supports custom logging templates via `ILoggable` interface

**Example**:
```csharp
public class CreateCategoryCommand : ICommand<Category>, ILoggable
{
    public Category Category { get; set; }

    public string LoggingTemplate => "Creating category: {CategoryName}";
    public object[] LoggingParameters => new object[] { Category.Name };
}
```

#### Caching Decorator (Queries Only)

**Purpose**: Cache query results to improve performance

**Usage**:
```csharp
builder.AddQueryMemoryCacheDecorator();
```

**How it works**:
1. Decorate your query with `[MemoryCache(ExpirationInSeconds = 300)]`
2. Implement `IGenerateCacheKey` to provide a unique cache key
3. The decorator checks the cache before executing the handler
4. Results are cached for the specified duration

**Example**:
```csharp
[MemoryCache(ExpirationInSeconds = 300)]
public class GetCategoryByIdQuery : IQuery<Category>, IGenerateCacheKey
{
    public int CategoryId { get; }

    public GetCategoryByIdQuery(int categoryId)
    {
        CategoryId = categoryId;
    }

    public string GetCacheKey() => $"Category-{CategoryId}";
}
```

**Advanced**: Implement `IGlobalCacheKeyPrefixProvider` for multi-tenant scenarios:
```csharp
public class TenantCacheKeyPrefixProvider : IGlobalCacheKeyPrefixProvider
{
    private readonly ITenantContext _tenantContext;

    public string GetGlobalCacheKeyPrefix() => $"Tenant-{_tenantContext.TenantId}";
}
```

#### Transaction Decorator (Commands Only)

**Purpose**: Wrap command execution in a database transaction

**Usage**:
```csharp
builder.AddCommandTransactionDecorator();
```

**How it works**:
1. Decorate your command with `[TransactionCommand]`
2. The decorator wraps the handler execution in a `TransactionScope`
3. If the handler succeeds, the transaction commits
4. If an exception occurs, the transaction rolls back

**Example**:
```csharp
[TransactionCommand]
public class CreateOrderCommand : ICommand<Order>
{
    // Command properties
}
```

### Working with RestMediator

The `RestMediator` simplifies REST API development by automatically mapping command/query results to HTTP responses.

**Key Concepts**:

1. **RestOperation** - Defines the type of REST operation (GetSingle, GetMany, Create, Update, Delete, etc.)
2. **Rules** - Define how operations map to HTTP status codes
3. **ContentResponse** - Controls what gets returned in the response body (None, Result, Full)

**Example Controller**:
```csharp
[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly IRestMediator _restMediator;

    public CategoryController(IRestMediator restMediator)
    {
        _restMediator = restMediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return await _restMediator.ProcessRestQueryAsync(
            RestOperation.GetMany,
            new GetCategoriesQuery());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        return await _restMediator.ProcessRestQueryAsync(
            RestOperation.GetSingle,
            new GetCategoryByIdQuery(id));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Category category)
    {
        return await _restMediator.ProcessRestCommandAsync(
            RestOperation.CreateWithContent,
            new CreateCategoryCommand(category));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Category category)
    {
        return await _restMediator.ProcessRestCommandAsync(
            RestOperation.UpdateWithContent,
            new UpdateCategoryCommand(id, category));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        return await _restMediator.ProcessRestCommandAsync(
            RestOperation.Delete,
            new DeleteCategoryCommand(id));
    }
}
```

**Default HTTP Status Codes**:
- `GetSingle` with result → 200 OK
- `GetSingle` with null → 404 Not Found
- `GetMany` → 200 OK (even if empty)
- `Create` → 201 Created
- `Update` → 200 OK or 204 No Content
- `Delete` → 200 OK or 204 No Content
- Validation failure → 400 Bad Request
- Exception → 500 Internal Server Error
- Cancelled request → 499 Client Closed Request

**See**: [RestMediator Documentation](Extensions/Minded.Extensions.WebApi/Readme.md)

### OData Support

Minded supports OData queries through the `Minded.Extensions.OData` package.

**Supported OData Features**:
- `$filter` - Filter results
- `$orderby` - Sort results
- `$top` - Limit results
- `$skip` - Skip results (pagination)
- `$count` - Include count
- `$expand` - Expand related entities

**Usage**:
```csharp
public class GetCategoriesQuery : IQuery<IQueryResponse<IEnumerable<Category>>>,
    ICanFilter, ICanOrderBy, ICanTop, ICanSkip, ICanCount, ICanExpand
{
    public Expression<Func<Category, bool>> Filter { get; set; }
    public IList<OrderDescriptor> OrderBy { get; set; }
    public int? Top { get; set; }
    public int? Skip { get; set; }
    public bool Count { get; set; }
    public string[] Expand { get; set; }
}

// In your controller
[HttpGet]
public async Task<IActionResult> Get(ODataQueryOptions<Category> queryOptions)
{
    var query = new GetCategoriesQuery();
    query.ApplyODataQueryOptions(queryOptions);
    return await _restMediator.ProcessRestQueryAsync(RestOperation.GetMany, query);
}
```

### Cancellation Token Support

Minded fully supports `CancellationToken` for graceful cancellation of long-running operations.

**Benefits**:
- Stops processing when client disconnects
- Respects request timeouts
- Improves resource utilization
- Returns HTTP 499 (Client Closed Request) instead of 500

**Usage**:
```csharp
public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
{
    return await _restMediator.ProcessRestQueryAsync(
        RestOperation.GetSingle,
        new GetCategoryByIdQuery(id),
        cancellationToken);
}
```

**In Handlers**:
```csharp
public async Task<Category> HandleAsync(
    GetCategoryByIdQuery query,
    CancellationToken cancellationToken = default)
{
    return await _context.Categories
        .SingleOrDefaultAsync(c => c.Id == query.CategoryId, cancellationToken);
}
```

---

## For Engineers Extending Minded

This section is for engineers who want to create custom decorators or extensions for the Minded framework.

### Creating a Custom Decorator

Decorators are the primary extension point in Minded. Here's how to create your own:

#### 1. Create the Decorator Class

**For Commands**:
```csharp
using Minded.Framework.Decorator;
using Minded.Framework.CQRS.Command;

public class MyCustomCommandDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>,
    ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private readonly ILogger _logger;

    public MyCustomCommandDecorator(
        ICommandHandler<TCommand> commandHandler,
        ILogger<MyCustomCommandDecorator<TCommand>> logger)
        : base(commandHandler)
    {
        _logger = logger;
    }

    public async Task<ICommandResponse> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default)
    {
        // Before handler execution
        _logger.LogInformation("Before executing {CommandType}", typeof(TCommand).Name);

        // Execute the next handler in the chain
        var response = await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);

        // After handler execution
        _logger.LogInformation("After executing {CommandType}", typeof(TCommand).Name);

        return response;
    }
}
```

**For Queries**:
```csharp
using Minded.Framework.Decorator;
using Minded.Framework.CQRS.Query;

public class MyCustomQueryDecorator<TQuery, TResult> : QueryHandlerDecoratorBase<TQuery, TResult>,
    IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    private readonly ILogger _logger;

    public MyCustomQueryDecorator(
        IQueryHandler<TQuery, TResult> queryHandler,
        ILogger<MyCustomQueryDecorator<TQuery, TResult>> logger)
        : base(queryHandler)
    {
        _logger = logger;
    }

    public async Task<TResult> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken = default)
    {
        // Before handler execution
        _logger.LogInformation("Before executing {QueryType}", typeof(TQuery).Name);

        // Execute the next handler in the chain
        var result = await InnerQueryHandler.HandleAsync(query, cancellationToken);

        // After handler execution
        _logger.LogInformation("After executing {QueryType}", typeof(TQuery).Name);

        return result;
    }
}
```

#### 2. Create an Extension Method for Registration

```csharp
using Minded.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static MindedBuilder AddMyCustomCommandDecorator(this MindedBuilder builder)
    {
        builder.QueueCommandDecoratorRegistrationAction((b, i) =>
            b.DecorateHandlerDescriptors(i, typeof(MyCustomCommandDecorator<>)));

        return builder;
    }

    public static MindedBuilder AddMyCustomQueryDecorator(this MindedBuilder builder)
    {
        builder.QueueQueryDecoratorRegistrationAction((b, i) =>
            b.DecorateHandlerDescriptors(i, typeof(MyCustomQueryDecorator<,>)));

        return builder;
    }
}
```

#### 3. Register Your Decorator

```csharp
services.AddMinded(assembly => assembly.Name.StartsWith("YourApp."), builder =>
{
    builder.AddMediator();

    builder.AddCommandValidationDecorator()
           .AddMyCustomCommandDecorator()      // Your custom decorator
           .AddCommandExceptionDecorator()
           .AddCommandLoggingDecorator()
           .AddCommandHandlers();

    builder.AddQueryValidationDecorator()
           .AddMyCustomQueryDecorator()        // Your custom decorator
           .AddQueryExceptionDecorator()
           .AddQueryLoggingDecorator()
           .AddQueryHandlers();
});
```

### Creating Attribute-Based Decorators

You can create decorators that only apply when a command/query has a specific attribute:

#### 1. Create the Attribute

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class MyCustomAttribute : Attribute
{
    public string CustomProperty { get; set; }
}
```

#### 2. Create the Decorator

```csharp
using System.ComponentModel;

public class MyCustomCommandDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>,
    ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<ICommandResponse> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if the command has the attribute
        var attribute = (MyCustomAttribute)TypeDescriptor
            .GetAttributes(command)[typeof(MyCustomAttribute)];

        if (attribute != null)
        {
            // Apply custom logic based on attribute
            Console.WriteLine($"Custom property: {attribute.CustomProperty}");
        }

        return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
    }
}
```

#### 3. Use the Attribute

```csharp
[MyCustom(CustomProperty = "SomeValue")]
public class CreateCategoryCommand : ICommand<Category>
{
    // Command properties
}
```

### Creating Decorator Attribute Validators

You can enforce that certain attributes require specific interfaces to be implemented:

```csharp
using Minded.Extensions.Configuration;

public class MyCustomAttributeValidator : IDecoratingAttributeValidator
{
    public void Validate(Func<AssemblyName, bool> assemblyFilter)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => assemblyFilter(a.GetName()))
            .ToList();

        foreach (var assembly in assemblies)
        {
            var typesWithAttribute = assembly.GetTypes()
                .Where(t => t.GetCustomAttributes(typeof(MyCustomAttribute), true).Any());

            foreach (var type in typesWithAttribute)
            {
                if (!typeof(IMyRequiredInterface).IsAssignableFrom(type))
                {
                    throw new InvalidOperationException(
                        $"Type {type.Name} has [MyCustom] attribute but doesn't implement IMyRequiredInterface");
                }
            }
        }
    }
}
```

Register the validator:
```csharp
services.AddSingleton<IDecoratingAttributeValidator, MyCustomAttributeValidator>();
```

### Creating Custom REST Rules

You can customize how RestMediator maps operations to HTTP responses:

```csharp
using Minded.Extensions.WebApi;

public class MyCustomRestRulesProvider : IRestRulesProvider
{
    public IEnumerable<ICommandRestRule> CommandRules => new List<ICommandRestRule>
    {
        new CommandRestRule
        {
            Operation = RestOperation.Create,
            StatusCode = HttpStatusCode.Created,
            ContentResponse = ContentResponse.Result,
            RuleConditionProperty = new RuleConditionProperty
            {
                PropertyName = nameof(ICommandResponse.Successful),
                ExpectedValue = true
            }
        },
        // Add more rules...
    };

    public IEnumerable<IQueryRestRule> QueryRules => new List<IQueryRestRule>
    {
        new QueryRestRule
        {
            Operation = RestOperation.GetSingle,
            StatusCode = HttpStatusCode.OK,
            ContentResponse = ContentResponse.Result
        },
        // Add more rules...
    };
}
```

Register your custom rules provider:
```csharp
services.AddScoped<IRestRulesProvider, MyCustomRestRulesProvider>();
```

### Best Practices for Extensions

1. **Single Responsibility** - Each decorator should do one thing well
2. **Order Matters** - Consider where your decorator should sit in the pipeline
3. **Cancellation Support** - Always pass `CancellationToken` through the chain
4. **Error Handling** - Let exceptions bubble up unless you have a specific reason to catch them
5. **Logging** - Use structured logging with meaningful context
6. **Performance** - Avoid heavy operations in decorators; they run on every request
7. **Testing** - Write unit tests for your decorators in isolation

---

## For Contributors

We welcome contributions to the Minded framework! Here's how you can help:

### Getting Started

1. **Fork the Repository**
   ```bash
   git clone https://github.com/norcino/Minded.git
   cd Minded
   ```

2. **Build the Solution**
   ```bash
   dotnet build
   ```

3. **Run Tests**
   ```bash
   dotnet test
   ```

### Project Structure

```
Minded/
├── Framework/                          # Core framework packages
│   ├── Minded.Framework.CQRS.Abstractions/
│   ├── Minded.Framework.Mediator/
│   └── Minded.Framework.Decorator/
├── Extensions/                         # Extension packages
│   ├── Minded.Extensions.WebApi/
│   ├── Minded.Extensions.Validation/
│   ├── Minded.Extensions.Logging/
│   ├── Minded.Extensions.Exception/
│   ├── Minded.Extensions.Caching.Memory/
│   └── Minded.Extensions.OData/
├── Example/                            # Example application
│   ├── Application.Api/
│   ├── Service.Category/
│   ├── Service.Transaction/
│   └── Tests/
└── Tests/                              # Framework tests
```

### Contribution Guidelines

#### Code Style

- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments to public APIs
- Keep methods focused and small

#### Writing Tests

- Write unit tests for all new features
- Aim for high code coverage
- Use descriptive test names: `MethodName_Scenario_ExpectedBehavior`
- Follow AAA pattern: Arrange, Act, Assert

Example:
```csharp
[TestMethod]
public async Task HandleAsync_WhenValidationFails_ReturnsValidationErrors()
{
    // Arrange
    var command = new CreateCategoryCommand(new Category { Name = "" });
    var validator = new CreateCategoryCommandValidator();

    // Act
    var result = await validator.ValidateAsync(command);

    // Assert
    Assert.IsFalse(result.IsValid);
    Assert.IsTrue(result.OutcomeEntries.Any());
}
```

#### Pull Request Process

1. **Create a Feature Branch**
   ```bash
   git checkout -b feature/my-new-feature
   ```

2. **Make Your Changes**
   - Write code
   - Add tests
   - Update documentation

3. **Commit Your Changes**
   ```bash
   git commit -m "feat: Add new feature description"
   ```

   Use conventional commit messages:
   - `feat:` - New feature
   - `fix:` - Bug fix
   - `docs:` - Documentation changes
   - `test:` - Adding tests
   - `refactor:` - Code refactoring
   - `chore:` - Maintenance tasks

4. **Push to Your Fork**
   ```bash
   git push origin feature/my-new-feature
   ```

5. **Create a Pull Request**
   - Provide a clear description of the changes
   - Reference any related issues
   - Ensure all tests pass
   - Wait for code review

#### Versioning

We follow [Semantic Versioning](https://semver.org/):
- **MAJOR** version for incompatible API changes
- **MINOR** version for new functionality in a backward-compatible manner
- **PATCH** version for backward-compatible bug fixes

#### Updating the Changelog

When making changes, update `Changelog.md` following the existing format:

```markdown
## X.Y.Z (YYYY-MM-DD)
Brief description of the release

### Affected
* List of affected packages

### Added
* New features

### Changed
* Changes to existing functionality

### Fixed
* Bug fixes
```

### Areas for Contribution

We're particularly interested in contributions in these areas:

- **New Decorators** - Transaction handling, retry logic, circuit breakers, etc.
- **Performance Improvements** - Optimizations to the decorator pipeline
- **Documentation** - Tutorials, examples, API documentation
- **Testing** - Increase test coverage, add integration tests
- **Bug Fixes** - Fix reported issues
- **Examples** - Real-world usage examples

### Questions or Issues?

- **Bug Reports**: [Open an issue](https://github.com/norcino/Minded/issues)
- **Feature Requests**: [Open an issue](https://github.com/norcino/Minded/issues)
- **Questions**: [Start a discussion](https://github.com/norcino/Minded/discussions)

---

## Example Application

The Minded repository includes a comprehensive example application that demonstrates real-world usage of the framework. This is an excellent resource for understanding how all the pieces fit together.

### What's Included

The example application is a simple **Category and Transaction Management API** that showcases:

- **RESTful API** using RestMediator
- **CQRS Pattern** with Commands and Queries
- **Validation** using the Validation decorator
- **Exception Handling** with proper logging
- **Retry Logic** for transient failure handling (demonstrated in CreateCategoryCommandHandler)
- **Caching** for query results
- **OData Support** for advanced querying
- **Entity Framework Core** integration
- **Automatic Database Seeding** for development
- **Unit Tests** and **Integration Tests**

### Project Structure

```
Example/
├── Application.Api/              # ASP.NET Core Web API
│   ├── Controllers/
│   │   ├── CategoryController.cs
│   │   └── TransactionController.cs
│   ├── Startup.cs               # Minded configuration
│   └── Program.cs
├── Service.Category/            # Category domain
│   ├── Command/
│   │   ├── CreateCategoryCommand.cs
│   │   ├── UpdateCategoryCommand.cs
│   │   └── DeleteCategoryCommand.cs
│   ├── CommandHandler/
│   │   ├── CreateCategoryCommandHandler.cs
│   │   ├── UpdateCategoryCommandHandler.cs
│   │   └── DeleteCategoryCommandHandler.cs
│   ├── Query/
│   │   ├── GetCategoriesQuery.cs
│   │   └── GetCategoryByIdQuery.cs
│   ├── QueryHandler/
│   │   ├── GetCategoriesQueryHandler.cs
│   │   └── GetCategoryByIdQueryHandler.cs
│   └── Validator/
│       ├── CreateCategoryCommandValidator.cs
│       └── CategoryValidator.cs
├── Service.Transaction/         # Transaction domain
│   └── (similar structure)
├── Data.Context/                # EF Core DbContext
│   ├── MindedExampleContext.cs
│   └── DatabaseSeeder.cs        # Automatic seeding
├── Data.Entity/                 # Domain entities
│   ├── Category.cs
│   ├── Transaction.cs
│   └── User.cs
└── Tests/                       # Unit and integration tests
    ├── Service.Category.Tests/
    ├── Service.Transaction.Tests/
    └── Application.Api.E2ETests/
```

### Running the Example

1. **Clone the Repository**
   ```bash
   git clone https://github.com/norcino/Minded.git
   cd Minded/Example
   ```

2. **Run the API**
   ```bash
   cd Application.Api
   dotnet run
   ```

3. **Access the API**
   - Swagger UI: `https://localhost:5001/swagger`
   - API Base URL: `https://localhost:5001/api`

4. **Try Some Requests**

   **Get all categories:**
   ```bash
   GET https://localhost:5001/api/category
   ```

   **Get a single category:**
   ```bash
   GET https://localhost:5001/api/category/1
   ```

   **Create a category:**
   ```bash
   POST https://localhost:5001/api/category
   Content-Type: application/json

   {
     "name": "Electronics"
   }
   ```

   **OData query:**
   ```bash
   GET https://localhost:5001/api/category?$filter=name eq 'Electronics'&$orderby=name&$top=10
   ```

### Key Examples to Study

#### 1. Simple Command with Validation

**Command**: `Service.Category/Command/CreateCategoryCommand.cs`
```csharp
[ValidateCommand]
public class CreateCategoryCommand : ICommand<Category>
{
    public Category Category { get; set; }
}
```

**Handler**: `Service.Category/CommandHandler/CreateCategoryCommandHandler.cs`
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

        return new CommandResponse<Category>(command.Category)
        {
            Successful = true
        };
    }
}
```

**Validator**: `Service.Category/Validator/CreateCategoryCommandValidator.cs`
```csharp
public class CreateCategoryCommandValidator : ICommandValidator<CreateCategoryCommand>
{
    public async Task<IValidationResult> ValidateAsync(CreateCategoryCommand command)
    {
        var result = new ValidationResult();

        if (command.Category == null)
        {
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(command.Category),
                "{0} is mandatory"));
        }

        return result;
    }
}
```

#### 2. Query with Caching

**Query**: `Service.Category/Query/GetCategoryByIdQuery.cs`
```csharp
[MemoryCache(ExpirationInSeconds = 300)]
public class GetCategoryByIdQuery : IQuery<Category>, IGenerateCacheKey
{
    public int CategoryId { get; }

    public string GetCacheKey() => $"Category-{CategoryId}";
}
```

#### 3. Query with OData Support

**Query**: `Service.Category/Query/GetCategoriesQuery.cs`
```csharp
[ValidateQuery]
public class GetCategoriesQuery : IQuery<IQueryResponse<IEnumerable<Category>>>,
    ICanCount, ICanTop, ICanSkip, ICanExpand, ICanOrderBy, ICanFilterExpression<Category>
{
    public bool Count { get; set; }
    public int? Top { get; set; }
    public int? Skip { get; set; }
    public string[] Expand { get; set; }
    public IList<OrderDescriptor> OrderBy { get; set; }
    public Expression<Func<Category, bool>> Filter { get; set; }
}
```

#### 4. Controller Using RestMediator

**Controller**: `Application.Api/Controllers/CategoryController.cs`
```csharp
[Route("api/[controller]")]
public class CategoryController : Controller
{
    private readonly IRestMediator _restMediator;

    [HttpGet]
    public async Task<IActionResult> Get(ODataQueryOptions<Category> queryOptions)
    {
        var query = new GetCategoriesQuery();
        query.ApplyODataQueryOptions(queryOptions);
        return await _restMediator.ProcessRestQueryAsync(RestOperation.GetMany, query);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Category category)
    {
        return await _restMediator.ProcessRestCommandAsync(
            RestOperation.CreateWithContent,
            new CreateCategoryCommand(category));
    }
}
```

### Database Seeding

The example application automatically seeds the database with sample data in development mode. This is perfect for testing and understanding the framework.

**See**: [Database Seeding Documentation](Example/Data.Context/README_DatabaseSeeding.md)

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
cd Tests/Service.Category.Tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

---

## Available Packages

All Minded packages are available on NuGet:

### Core Framework

| Package | Version | Description |
|---------|---------|-------------|
| [Minded.Framework.CQRS.Abstractions](https://www.nuget.org/packages/Minded.Framework.CQRS.Abstractions/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Framework.CQRS.Abstractions.svg) | Core CQRS interfaces (ICommand, IQuery, ICommandHandler, IQueryHandler) |
| [Minded.Framework.Mediator.Abstractions](https://www.nuget.org/packages/Minded.Framework.Mediator.Abstractions/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Framework.Mediator.Abstractions.svg) | Mediator interfaces |
| [Minded.Framework.Mediator](https://www.nuget.org/packages/Minded.Framework.Mediator/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Framework.Mediator.svg) | Mediator implementation |
| [Minded.Framework.Decorator](https://www.nuget.org/packages/Minded.Framework.Decorator/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Framework.Decorator.svg) | Base classes for decorators |

### Extensions

| Package | Version | Description |
|---------|---------|-------------|
| [Minded.Extensions.WebApi](https://www.nuget.org/packages/Minded.Extensions.WebApi/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.WebApi.svg) | RestMediator for RESTful APIs |
| [Minded.Extensions.Validation](https://www.nuget.org/packages/Minded.Extensions.Validation/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.Validation.svg) | Validation decorator and interfaces |
| [Minded.Extensions.Logging](https://www.nuget.org/packages/Minded.Extensions.Logging/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.Logging.svg) | Logging decorator |
| [Minded.Extensions.Exception](https://www.nuget.org/packages/Minded.Extensions.Exception/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.Exception.svg) | Exception handling decorator |
| [Minded.Extensions.Retry](https://www.nuget.org/packages/Minded.Extensions.Retry/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.Retry.svg) | Retry decorator for transient failure handling |
| [Minded.Extensions.Caching.Memory](https://www.nuget.org/packages/Minded.Extensions.Caching.Memory/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.Caching.Memory.svg) | In-memory caching decorator |
| [Minded.Extensions.Transaction](https://www.nuget.org/packages/Minded.Extensions.Transaction/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.Transaction.svg) | Transaction decorator |
| [Minded.Extensions.OData](https://www.nuget.org/packages/Minded.Extensions.OData/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.OData.svg) | OData query support |

---

## Documentation

### Framework Documentation

- **[Changelog](Changelog.md)** - Version history and release notes
- **[RestMediator Guide](Extensions/Minded.Extensions.WebApi/Readme.md)** - Comprehensive RestMediator documentation
- **[Cancellation Handling](Extensions/Minded.Extensions.Exception/README_CancellationHandling.md)** - CancellationToken support and best practices
- **[Database Seeding](Example/Data.Context/README_DatabaseSeeding.md)** - Automatic database seeding for development

### External Resources

- **[GitHub Repository](https://github.com/norcino/Minded)** - Source code and issues
- **[NuGet Packages](https://www.nuget.org/packages?q=Minded)** - All published packages
- **[Build Status](https://dev.azure.com/norcino/Minded/_build)** - CI/CD pipeline

### Design Patterns

- [Mediator Pattern](https://en.wikipedia.org/wiki/Mediator_pattern)
- [Command Pattern](https://en.wikipedia.org/wiki/Command_pattern)
- [Decorator Pattern](https://en.wikipedia.org/wiki/Decorator_pattern)
- [CQRS](https://martinfowler.com/bliki/CQRS.html)

---

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

```
MIT License

Copyright (c) 2023 Manuel Salvatori

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## Support and Community

- 🐛 **Found a bug?** [Open an issue](https://github.com/norcino/Minded/issues)
- 💡 **Have an idea?** [Start a discussion](https://github.com/norcino/Minded/discussions)
- 📖 **Need help?** Check the [example application](#example-application) or [open an issue](https://github.com/norcino/Minded/issues)
- ⭐ **Like the project?** Give it a star on [GitHub](https://github.com/norcino/Minded)!

---

**Made with ❤️ by [Manuel Salvatori](https://github.com/norcino)**

