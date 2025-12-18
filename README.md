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
- [Performance Considerations](#performance-considerations)
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
- **Sensitive Data Protection** - Automatic PII/confidential data protection in logs for GDPR/CCPA compliance
- **Extensibility** - Add your own decorators for custom cross-cutting concerns
- **Centralized Configuration** - All decorators and handlers are registered in one place
- **Vast Ecosystem** - Leverage existing libraries for validation, logging, caching, transaction, retry, etc.

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

    // Configure Command pipeline
    // IMPORTANT: Decorators are registered from INNERMOST (closest to handler) to OUTERMOST
    // Execution order: Exception → Logging → Validation → Handler
    builder.AddCommandValidationDecorator()    // Innermost - executes closest to handler
           .AddCommandLoggingDecorator()       // Middle - wraps validation
           .AddCommandExceptionDecorator()     // Outermost - wraps everything
           .AddCommandHandlers();              // Registers handlers and applies decorators

    // Configure Query pipeline
    // Execution order: Exception → Logging → Caching → Validation → Handler
    builder.AddQueryValidationDecorator()      // Innermost
           .AddQueryMemoryCacheDecorator()     // Optional: Add caching
           .AddQueryLoggingDecorator()         // Middle
           .AddQueryExceptionDecorator()       // Outermost
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

### Understanding Decorator Order

**CRITICAL**: The order in which you register decorators determines the execution flow. Decorators are registered from **INNERMOST to OUTERMOST**.

#### Registration Order vs Execution Order

When you register decorators like this:
```csharp
builder.AddCommandValidationDecorator()    // 1st registered
       .AddCommandLoggingDecorator()       // 2nd registered
       .AddCommandExceptionDecorator()     // 3rd registered
       .AddCommandHandlers();
```

The **execution order is REVERSED**:
```
Request
    ↓
ExceptionDecorator (3rd registered = OUTERMOST = executes FIRST)
    ↓
LoggingDecorator (2nd registered = MIDDLE)
    ↓
ValidationDecorator (1st registered = INNERMOST = executes LAST before handler)
    ↓
Handler
```

#### Why This Matters

The decorator closest to the handler (registered first) executes **inside** all outer decorators. This is crucial for:

1. **Validation Inside Transactions**: If you need validation to read database state with transaction isolation:
   ```csharp
   builder.AddCommandValidationDecorator()      // Executes inside transaction
          .AddCommandTransactionDecorator()     // Wraps validation
          .AddCommandHandlers();
   ```

2. **Exception Handling Wraps Everything**: Exception decorator should be outermost to catch all errors:
   ```csharp
   builder.AddCommandValidationDecorator()
          .AddCommandLoggingDecorator()
          .AddCommandExceptionDecorator()       // Catches all exceptions
          .AddCommandHandlers();
   ```

3. **Logging Captures Transaction Lifecycle**: Logging should wrap transactions to log start/commit/rollback:
   ```csharp
   builder.AddCommandTransactionDecorator()     // Transaction executes inside logging
          .AddCommandLoggingDecorator()         // Logs transaction lifecycle
          .AddCommandExceptionDecorator()
          .AddCommandHandlers();
   ```

#### Recommended Order for Commands

```csharp
// For commands with transactions and stateful validation (reads from DB)
builder.AddCommandValidationDecorator()      // Innermost - validates inside transaction
       .AddCommandTransactionDecorator()     // Wraps validation - provides isolation
       .AddCommandLoggingDecorator()         // Logs transaction execution
       .AddCommandExceptionDecorator()       // Outermost - catches all errors
       .AddCommandHandlers();

// For commands with stateless validation (no DB reads)
builder.AddCommandTransactionDecorator()     // Innermost - transaction only wraps handler
       .AddCommandValidationDecorator()      // Validates before transaction (fail fast)
       .AddCommandLoggingDecorator()
       .AddCommandExceptionDecorator()       // Outermost
       .AddCommandHandlers();
```

#### Recommended Order for Queries

```csharp
builder.AddQueryValidationDecorator()        // Innermost
       .AddQueryMemoryCacheDecorator()       // Cache after validation
       .AddQueryLoggingDecorator()           // Log cache hits/misses
       .AddQueryExceptionDecorator()         // Outermost - catches all errors
       .AddQueryHandlers();
```

---

### Decorator Configuration Options

Many decorators accept configuration options when registered via extension methods (e.g., `AddCommandLoggingDecorator(options => ...)`, `AddDataProtection(options => ...)`, `AddCommandTransactionDecorator(options => ...)`). These options control the decorator's runtime behavior.

#### Configuration Pattern Convention

Minded follows a **consistent configuration pattern** across all decorators that support options:

**1. Static Configuration Properties**

Static properties define fixed configuration values:

```csharp
builder.AddCommandLoggingDecorator(options =>
{
    options.Enabled = true;                              // Static boolean
    options.MinimumOutcomeSeverityLevel = Severity.Warning;  // Static enum
});
```

**2. Provider Properties (Dynamic Configuration)**

For every static configuration property, there is a corresponding **Provider property** that accepts a `Func<T>` delegate. This allows **dynamic runtime configuration** based on feature flags, external configuration, database values, environment variables, etc.

**Naming Convention**: `{PropertyName}Provider`

```csharp
builder.AddCommandLoggingDecorator(options =>
{
    // Static value
    options.MinimumOutcomeSeverityLevel = Severity.Warning;

    // Dynamic provider (takes precedence over static value)
    options.MinimumOutcomeSeverityLevelProvider = () =>
    {
        // Read from feature flag service
        if (_featureFlags.IsEnabled("DetailedLogging"))
            return Severity.Info;

        // Read from external configuration
        return _configService.GetValue<Severity>("Logging:MinSeverity", Severity.Warning);
    };
});
```

**3. Provider Precedence**

When both a static property and its provider are configured, the **provider takes precedence**:

```csharp
options.Enabled = true;  // Static: always enabled
options.EnabledProvider = () => !_environment.IsProduction();  // Provider wins: disabled in production
```

#### Common Configuration Scenarios

**Environment-Based Configuration**:
```csharp
builder.AddCommandExceptionDecorator(options =>
{
    // Serialize command/query details only in development
    options.SerializeProvider = () => _environment.IsDevelopment();
});
```

**Feature Flag-Based Configuration**:
```csharp
builder.AddCommandLoggingDecorator(options =>
{
    // Enable detailed logging based on feature flag
    options.LogOutcomeEntriesProvider = () => _featureFlags.IsEnabled("DetailedLogging");
});
```

**Database/External Configuration**:
```csharp
builder.AddDataProtection(options =>
{
    // Read sensitive data visibility from database configuration
    options.ShowSensitiveDataProvider = () => _configRepository.GetBoolAsync("ShowSensitiveData").Result;
});
```

**Multi-Tenant Configuration**:
```csharp
builder.AddCommandLoggingDecorator(options =>
{
    // Different log levels per tenant
    options.MinimumOutcomeSeverityLevelProvider = () =>
    {
        var tenantId = _tenantContext.TenantId;
        return _tenantConfigService.GetLogLevel(tenantId);
    };
});
```

#### Configuration via appsettings.json

Most decorators also support configuration via `appsettings.json` when using the parameterless registration method:

```json
{
  "Minded": {
    "LoggingOptions": {
      "Enabled": true,
      "LogMessageTemplateData": true,
      "MinimumOutcomeSeverityLevel": "Warning"
    },
    "TransactionOptions": {
      "DefaultIsolationLevel": "ReadCommitted",
      "DefaultTimeout": "00:02:00",
      "RollbackOnUnsuccessfulResponse": true
    },
    "DataProtectionOptions": {
      "ShowSensitiveData": false
    }
  }
}
```

**Note**: Provider properties cannot be configured via `appsettings.json` - they must be set programmatically during service registration.

#### Decorators with Configuration Options

The following decorators accept configuration options:

| Decorator | Options Class | Supports Providers | appsettings.json Support |
|-----------|---------------|-------------------|-------------------------|
| **Logging** | `LoggingOptions` | ✅ Yes | ✅ Yes |
| **Transaction** | `TransactionOptions` | ❌ No | ✅ Yes |
| **Exception** | `ExceptionOptions` | ✅ Yes | ❌ No |
| **Data Protection** | `DataProtectionOptions` | ✅ Yes | ✅ Yes |

**Decorators without Configuration Options**:
- **Validation** - Configured via `[ValidateCommand]`/`[ValidateQuery]` attributes and FluentValidation validators
- **Retry** - Configured via `[RetryCommand]`/`[RetryQuery]` attributes on commands/queries
- **Caching** - Configured via `[MemoryCache]` attribute and `IGlobalCacheKeyPrefixProvider`
- **WebApi/RestMediator** - Configured via `IRestRulesProvider` implementation

For detailed configuration options for each decorator, see the individual decorator documentation linked below.

---

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
- Logs exceptions with full context (command/query type, properties, exception details)
- Distinguishes between `OperationCanceledException` (logged as Information) and real errors (logged as Error)
- Wraps exceptions in `CommandHandlerException` or `QueryHandlerException`
- **Centralized logging sanitization pipeline** - Automatically removes non-serializable types and sensitive data from logs
- Extensible sanitization - Add custom sanitizers to control what gets logged

**See**: [Exception Handling Documentation](Extensions/Minded.Extensions.Exception/README.md)

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

**See**: [Validation Decorator Documentation](Extensions/Minded.Extensions.Validation/README.md)

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

**See**: [Retry Decorator Documentation](Extensions/Minded.Extensions.Retry/README.md)

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
    public string[] LoggingProperties => new[] { "Category.Name" };
}
```

**See**: [Logging Decorator Documentation](Extensions/Minded.Extensions.Logging/README.md)

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

**See**: [Caching Decorator Documentation](Extensions/Minded.Extensions.Caching.Memory/README.md)

#### Transaction Decorator

**Purpose**: Wrap command execution in a database transaction with automatic rollback on failure

**Package**: `Minded.Extensions.Transaction`

**Usage**:
```csharp
// Basic registration
builder.AddCommandTransactionDecorator();

// With configuration
builder.AddCommandTransactionDecorator(options =>
{
    options.DefaultIsolationLevel = IsolationLevel.ReadCommitted;
    options.DefaultTimeout = TimeSpan.FromMinutes(2);
    options.RollbackOnUnsuccessfulResponse = true;  // Rollback if Successful = false
    options.EnableLogging = true;                    // Log transaction lifecycle
});

// Query support (rarely needed - see warnings below)
builder.AddQueryTransactionDecorator();
```

**How it works**:
1. Decorate your command with `[TransactionalCommand]` or query with `[TransactionalQuery]`
2. The decorator wraps execution in a `System.Transactions.TransactionScope`
3. Nested commands/queries automatically join the ambient transaction
4. Transaction commits if handler succeeds and returns `Successful = true`
5. Transaction rolls back if:
   - An exception is thrown
   - Handler returns `Successful = false` (if `RollbackOnUnsuccessfulResponse = true`)

**Basic Example**:
```csharp
[TransactionalCommand]
public class CreateOrderCommand : ICommand<Order>
{
    public Order Order { get; set; }
}

public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Order>
{
    private readonly IDbContext _context;
    private readonly IMediator _mediator;

    public async Task<ICommandResponse<Order>> HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        // Save order (within transaction)
        await _context.Orders.AddAsync(command.Order, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Nested command - automatically joins the same transaction
        await _mediator.ProcessCommandAsync(
            new UpdateInventoryCommand { OrderId = command.Order.Id },
            cancellationToken);

        // If any operation fails, entire transaction rolls back
        return new CommandResponse<Order>(command.Order) { Successful = true };
    }
}
```

**Advanced Configuration**:
```csharp
// Per-command configuration with custom isolation level and timeout
[TransactionalCommand(
    IsolationLevel = IsolationLevel.Serializable,  // Strongest isolation
    TimeoutSeconds = 300)]                          // 5 minutes
public class ProcessPaymentCommand : ICommand<Payment>
{
    public decimal Amount { get; set; }
}

// Nested transaction with different isolation level (creates new transaction)
[TransactionalCommand(
    TransactionScopeOption = TransactionScopeOption.RequiresNew,
    IsolationLevel = IsolationLevel.ReadUncommitted)]
public class AuditLogCommand : ICommand
{
    // This always creates a new transaction, even if called from another transaction
}

// Suppress transaction (execute outside any transaction)
[TransactionalCommand(TransactionScopeOption = TransactionScopeOption.Suppress)]
public class SendEmailCommand : ICommand
{
    // This executes outside any transaction
}
```

**Nested Transaction Behavior**:

The decorator automatically handles nested transactions using `TransactionScopeOption`:

- **`Required` (default)**: Joins existing transaction or creates new one
  ```csharp
  [TransactionalCommand]  // Creates transaction
  public class OuterCommand : ICommand
  {
      public async Task HandleAsync(...)
      {
          // Inner command joins the same transaction
          await _mediator.ProcessCommandAsync(new InnerCommand());
      }
  }

  [TransactionalCommand]  // Joins outer transaction (doesn't create new)
  public class InnerCommand : ICommand { }
  ```

- **`RequiresNew`**: Always creates new transaction (suspends outer transaction)
  ```csharp
  [TransactionalCommand(TransactionScopeOption = TransactionScopeOption.RequiresNew)]
  public class AuditCommand : ICommand
  {
      // Always creates new transaction
      // Commits independently even if outer transaction rolls back
  }
  ```

- **`Suppress`**: Executes outside any transaction
  ```csharp
  [TransactionalCommand(TransactionScopeOption = TransactionScopeOption.Suppress)]
  public class NotificationCommand : ICommand
  {
      // Executes outside transaction
      // Useful for operations that shouldn't be rolled back
  }
  ```

**Isolation Levels**:

Control transaction isolation to prevent race conditions:

```csharp
// ReadCommitted (default) - Prevents dirty reads
[TransactionalCommand(IsolationLevel = IsolationLevel.ReadCommitted)]
public class CreateOrderCommand : ICommand<Order> { }

// RepeatableRead - Prevents dirty reads and non-repeatable reads
// Use when validation reads DB state that must not change during execution
[TransactionalCommand(IsolationLevel = IsolationLevel.RepeatableRead)]
public class CancelOrderCommand : ICommand
{
    // Validator reads Order.Status
    // Handler updates Order.Status
    // RepeatableRead ensures status doesn't change between validation and update
}

// Serializable - Strongest isolation (prevents all anomalies)
[TransactionalCommand(IsolationLevel = IsolationLevel.Serializable)]
public class ProcessPaymentCommand : ICommand<Payment>
{
    // Use for critical financial operations
    // Warning: Can cause deadlocks and performance issues
}
```

**Rollback Strategies**:

```csharp
// Strategy 1: Rollback only on exception (default if RollbackOnUnsuccessfulResponse = false)
builder.AddCommandTransactionDecorator(options =>
{
    options.RollbackOnUnsuccessfulResponse = false;
});

// Strategy 2: Rollback on unsuccessful response (recommended)
builder.AddCommandTransactionDecorator(options =>
{
    options.RollbackOnUnsuccessfulResponse = true;  // Default
});

// Handler can control rollback via Successful property
public async Task<ICommandResponse> HandleAsync(CreateOrderCommand command, ...)
{
    if (inventoryNotAvailable)
    {
        return new CommandResponse
        {
            Successful = false  // Transaction will roll back
        };
    }

    return new CommandResponse { Successful = true };  // Transaction commits
}
```

** Important Limitations**:

The transaction decorator **ONLY** covers database operations. It **DOES NOT** roll back:

- Remote service calls (HTTP, gRPC, etc.)
- Message queue operations (RabbitMQ, Azure Service Bus, etc.)
- File system operations
- External API calls
- Email sending
- Cache updates (unless using transactional cache)

**Example of what gets rolled back vs what doesn't**:
```csharp
[TransactionalCommand]
public class CreateOrderCommand : ICommand<Order>
{
    public async Task HandleAsync(...)
    {
        // WILL BE ROLLED BACK on error
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        // WILL NOT BE ROLLED BACK on error
        await _httpClient.PostAsync("https://api.payment.com/charge", ...);
        await _serviceBusClient.SendMessageAsync(new OrderCreatedMessage());
        await _emailService.SendOrderConfirmationAsync(order);

        throw new Exception("Error!");
        // Database changes roll back
        // Payment charge, message, and email are NOT rolled back!
    }
}
```

**For distributed scenarios, use**:
- **Saga Pattern** - Orchestrate compensating transactions
- **Transactional Outbox Pattern** - Ensure message delivery with database changes
- **Two-Phase Commit** - For distributed transactions (complex, avoid if possible)

**When to Use Transaction Decorator**:

**Use for**:
- Multiple database operations that must succeed/fail together
- Commands that invoke nested commands/queries (all share same transaction)
- Operations requiring consistent database state (use appropriate isolation level)
- Financial operations requiring atomicity

**Don't use for**:
- Single database operation (DbContext already uses transaction)
- Read-only queries (unless you need consistent snapshot with specific isolation level)
- Operations involving external services (use Saga pattern instead)
- Long-running operations (risk of locks and timeouts)

**Decorator Order Considerations**:

```csharp
// If validation reads from database and needs transaction isolation:
builder.AddCommandValidationDecorator()      // Validates INSIDE transaction
       .AddCommandTransactionDecorator()     // Wraps validation
       .AddCommandLoggingDecorator()
       .AddCommandExceptionDecorator()
       .AddCommandHandlers();

// If validation is stateless (no DB reads):
builder.AddCommandTransactionDecorator()     // Transaction only wraps handler
       .AddCommandValidationDecorator()      // Validates BEFORE transaction (fail fast)
       .AddCommandLoggingDecorator()
       .AddCommandExceptionDecorator()
       .AddCommandHandlers();
```

**Query Transaction Support** (Rarely Needed):

Queries can use transactions for consistent snapshots:

```csharp
builder.AddQueryTransactionDecorator();

[TransactionalQuery(IsolationLevel = IsolationLevel.Snapshot)]
public class GetOrderWithItemsQuery : IQuery<OrderDto>
{
    // Ensures consistent snapshot of Order and OrderItems
    // Even if other transactions are modifying data
}
```

**Warning**: Most queries don't need transactions. Only use when you need:
- Consistent snapshot across multiple tables
- Specific isolation level to prevent read anomalies
- Read locks to prevent updates during query execution

**See**: [Transaction Decorator Documentation](Extensions/Minded.Extensions.Transaction/README.md)

#### WebApi Decorator - Working with RestMediator

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

#### Data Protection Decorator - Sensitive Data Protection

Minded now includes built-in protection for sensitive data in logs, helping you comply with GDPR, CCPA, and other privacy regulations.

**Mark sensitive properties with a simple attribute:**

```csharp
using Minded.Extensions.DataProtection.Abstractions;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }

    [SensitiveData]  // Automatically hidden from logs
    public string Email { get; set; }

    [SensitiveData]  // Automatically hidden from logs
    public string Surname { get; set; }
}
```

**Configure DataProtection based on environment:**

```csharp
services.AddMinded(Configuration, assembly => assembly.Name.StartsWith("Service."), builder =>
{
    // Add DataProtection with environment-based configuration
    builder.AddDataProtection(options =>
    {
        // Show sensitive data only in development
        options.ShowSensitiveDataProvider = () => _environment.IsDevelopment();
    });

    // Add decorators that will use DataProtection
    builder.AddLogging();
    builder.AddExceptionHandling();
});
```

**What gets protected:**
- Properties marked with `[SensitiveData]` are omitted from logs by default
- Works recursively on nested objects
- Applies to both logging and exception decorators when DataProtection is configured
- Secure by default - sensitive data is hidden in production
- Optional - Logging and Exception decorators work without DataProtection installed

**Learn more:** See [DataProtection Documentation](Extensions/Minded.Extensions.DataProtection/README.md) for complete details.

---

# Work in progress
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

### Understanding Decorator Base Classes

Minded provides base classes that simplify decorator creation:

#### CommandHandlerDecoratorBase<TCommand>

Base class for command decorators. Provides access to the next handler in the chain.

**Key Properties:**
- `DecoratedCommmandHandler` - The next command handler in the decorator chain (note: typo in property name is intentional for backward compatibility)

**Usage:**
```csharp
public class MyCommandDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public MyCommandDecorator(ICommandHandler<TCommand> commandHandler) : base(commandHandler)
    {
    }

    public async Task<ICommandResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        // Your logic before
        var response = await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
        // Your logic after
        return response;
    }
}
```

#### QueryHandlerDecoratorBase<TQuery, TResult>

Base class for query decorators. Provides access to the next handler in the chain.

**Key Properties:**
- `InnerQueryHandler` - The next query handler in the decorator chain

**Usage:**
```csharp
public class MyQueryDecorator<TQuery, TResult> : QueryHandlerDecoratorBase<TQuery, TResult>, IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public MyQueryDecorator(IQueryHandler<TQuery, TResult> queryHandler) : base(queryHandler)
    {
    }

    public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
    {
        // Your logic before
        var result = await InnerQueryHandler.HandleAsync(query, cancellationToken);
        // Your logic after
        return result;
    }
}
```

### Creating a Custom Decorator

Decorators are the primary extension point in Minded. Here's how to create your own:

#### 1. Create the Decorator Class

**For Commands**:
```csharp
using Minded.Framework.Decorator;
using Minded.Framework.CQRS.Command;

public class MyCustomCommandDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand> where TCommand : ICommand
{
    private readonly ILogger _logger;

    public MyCustomCommandDecorator(ICommandHandler<TCommand> commandHandler, ILogger<MyCustomCommandDecorator<TCommand>> logger) : base(commandHandler)
    {
        _logger = logger;
    }

    public async Task<ICommandResponse> HandleAsync( TCommand command, CancellationToken cancellationToken = default)
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

public class MyCustomQueryDecorator<TQuery, TResult> : QueryHandlerDecoratorBase<TQuery, TResult>, IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    private readonly ILogger _logger;

    public MyCustomQueryDecorator(IQueryHandler<TQuery, TResult> queryHandler, ILogger<MyCustomQueryDecorator<TQuery, TResult>> logger) : base(queryHandler)
    {
        _logger = logger;
    }

    public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
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

public class MyCustomCommandDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand> where TCommand : ICommand
{
    public async Task<ICommandResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        // Check if the command has the attribute
        var attribute = (MyCustomAttribute)TypeDescriptor.GetAttributes(command)[typeof(MyCustomAttribute)];

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

You can enforce that certain attributes require specific interfaces to be implemented, for example the Minded.Extensions.Caching requires IGenerateCacheKey to be implemented when the attibrute is applied:

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

The attribute validator classes should not be manually registered with the dependency injection container, they will be loaded automatically.

### Logging Sanitizers
The framework provides a centralized logging sanitization pipeline that processes commands and queries before logging them.
If the framework is extended adding logging, auditing or telemetry capabilities, it is recommended to use the logging sanitization pipeline to sanitize the commands and queries before logging them. \
This will guarantee that the sensitive data is protected and that the logging output is consistent across all the framework.

#### 1. Add a custom sanitizer along with a decorator

You can create custom sanitizers to control what gets logged. Implement the `ILoggingSanitizer` interface:

```csharp
using Minded.Framework.CQRS.Abstractions.Sanitization;

public class MyCustomSanitizer : ILoggingSanitizer
{
    public IDictionary<string, object> Sanitize(IDictionary<string, object> data, Type sourceType)
    {
        // Your custom sanitization logic
    }
}
```

Register using ```RegisterLoggingSanitizerPipelineConfiguration``` in ```MindedBuilder``` it is possible to add a new sanitizer to the pipeline, which will be executed befure the logging output is generated, it is possible also to register specific properties to be excluded from the logging output:

```csharp
services.AddMinded(Configuration, assembly => assembly.Name.StartsWith("Service."), builder =>
{
    builder.RegisterLoggingSanitizerPipelineConfiguration(pipeline =>
    {
        pipeline.RegisterSanitizer(new MyCustomSanitizer());
    });

    // Other configuration...
});
```

Once the sanitizer is added to the pipeline, this will be used across all the framework where the pipeline is correctly used.

#### 2. Usage of the framework logging pipeline
In order to leverage the logging sanitization pipeline, when creating a new decorator you need to inject the ```LoggingSanitizerPipeline``` and use the ```Sanitize``` method to sanitize the command or query before logging it:

```csharp
using Minded.Framework.CQRS.Abstractions.Sanitization;

public class MyCustomCommandDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand> where TCommand : ICommand
{
    private readonly ILoggingSanitizerPipeline _sanitizerPipeline;

    public MyCustomCommandDecorator(ILoggingSanitizerPipeline sanitizerPipeline, ICommandHandler<TCommand> decoratedCommmandHandler) : base(decoratedCommmandHandler)
    {
        _sanitizerPipeline = sanitizerPipeline;
    }

    public async Task<ICommandResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        // Sanitize the command before logging
        var sanitizedCommand = _sanitizerPipeline.Sanitize(command);

        // Use the sanitized command for logging, auditing, streaming etc..

        // Execute the next handler in the chain
        return await InnerCommandHandler.HandleAsync(command, cancellationToken);
    }
}
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

### Understanding Decorator Execution Order

**CRITICAL**: Decorators are registered from **INNERMOST to OUTERMOST**, but execute in **REVERSE order**.

#### Registration Order (Innermost → Outermost)

```csharp
services.AddMinded(builder =>
{
    builder.AddCommandValidationDecorator()    // 1. Registered FIRST (innermost)
           .AddCommandRetryDecorator()          // 2. Registered SECOND
           .AddCommandLoggingDecorator()        // 3. Registered THIRD
           .AddCommandExceptionDecorator()      // 4. Registered LAST (outermost)
           .AddCommandHandlers();               // 5. Actual handler (core)
});
```

#### Execution Order (Outermost → Innermost → Handler → Innermost → Outermost)

```
Request Flow:
1. Exception Decorator (outermost) - ENTERS
2. Logging Decorator - ENTERS
3. Retry Decorator - ENTERS
4. Validation Decorator (innermost) - ENTERS
5. Handler - EXECUTES
6. Validation Decorator - EXITS
7. Retry Decorator - EXITS
8. Logging Decorator - EXITS
9. Exception Decorator - EXITS
```

**Visual Representation:**

```
┌─────────────────────────────────────────────────────────┐
│ Exception Decorator (catches all errors)                │
│  ┌───────────────────────────────────────────────────┐  │
│  │ Logging Decorator (logs execution)                │  │
│  │  ┌─────────────────────────────────────────────┐  │  │
│  │  │ Retry Decorator (retries on failure)       │  │  │
│  │  │  ┌───────────────────────────────────────┐  │  │  │
│  │  │  │ Validation Decorator (validates)     │  │  │  │
│  │  │  │  ┌─────────────────────────────────┐  │  │  │  │
│  │  │  │  │ Handler (business logic)        │  │  │  │  │
│  │  │  │  └─────────────────────────────────┘  │  │  │  │
│  │  │  └───────────────────────────────────────┘  │  │  │
│  │  └─────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

#### Why This Matters

1. **Exception Handling** - Should be outermost (registered last) to catch all errors
2. **Validation** - Should be innermost (registered first) to fail fast
3. **Logging** - Should wrap most operations to log everything
4. **Retry** - Should be inside exception handling but outside validation

**Example - Correct Order:**

```csharp
builder.AddCommandValidationDecorator()      // Validates first (fail fast)
       .AddCommandRetryDecorator()           // Retries if handler fails
       .AddCommandLoggingDecorator()         // Logs all attempts
       .AddCommandExceptionDecorator()       // Catches all exceptions
       .AddCommandHandlers();
```

**Example - Incorrect Order:**

```csharp
builder.AddCommandExceptionDecorator()       // ❌ Catches exceptions too early
       .AddCommandValidationDecorator()      // ❌ Validation errors not caught
       .AddCommandHandlers();
```

### Accessing Dependency Injection Services in Decorators

Decorators can access any service registered in the DI container:

```csharp
public class MyCustomCommandDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private readonly ILogger<MyCustomCommandDecorator<TCommand>> _logger;
    private readonly IMyCustomService _customService;
    private readonly IConfiguration _configuration;

    public MyCustomCommandDecorator(
        ICommandHandler<TCommand> commandHandler,
        ILogger<MyCustomCommandDecorator<TCommand>> logger,
        IMyCustomService customService,
        IConfiguration configuration) : base(commandHandler)
    {
        _logger = logger;
        _customService = customService;
        _configuration = configuration;
    }

    public async Task<ICommandResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        // Use injected services
        var setting = _configuration["MySetting"];
        await _customService.DoSomethingAsync();

        return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
    }
}
```

**Important**: The first parameter MUST be the decorated handler (`ICommandHandler<TCommand>` or `IQueryHandler<TQuery, TResult>`). All other parameters are resolved from DI.

### Best Practices for Extensions

1. **Single Responsibility** - Each decorator should do one thing well
2. **Order Matters** - Consider where your decorator should sit in the pipeline (see execution order above)
3. **Cancellation Support** - Always pass `CancellationToken` through the chain
4. **Error Handling** - Let exceptions bubble up unless you have a specific reason to catch them
5. **Logging** - Use structured logging with meaningful context
6. **Performance** - Avoid heavy operations in decorators; they run on every request
7. **Testing** - Write unit tests for your decorators in isolation
8. **DI Services** - Inject services via constructor, but decorated handler must be first parameter
9. **Async/Await** - Always use async/await properly; don't block with `.Result` or `.Wait()`
10. **Sanitization** - Use the logging sanitization pipeline for any logging/auditing to protect sensitive data

---

## For Contributors

We welcome contributions to the Minded framework! Here's how you can help:

### Getting Started

1. **Fork the Repository**
   ````powershell
   git clone https://github.com/norcino/Minded.git
   cd Minded
   ````

2. **Build the Solution**
   ````bash
   dotnet build
   ````

3. **Run Tests**
   ````bash
   dotnet test
   ````

### Project Structure

````
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
````

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

## Performance Considerations

The Minded Framework is designed with performance in mind, but understanding how the decorator pipeline works will help you build efficient applications.

### Decorator Pipeline Overhead

Each decorator adds a small amount of overhead to request processing. While this is typically negligible, consider the following:

- **Decorator Order Matters** - Place lightweight decorators (like logging) before heavy ones (like validation)
- **Avoid Heavy Operations** - Decorators run on every request; keep them fast and focused
- **Use Async/Await Properly** - All handlers and decorators support async operations; use them for I/O-bound work
- **Caching Strategy** - Use the caching decorator for expensive queries, but be mindful of cache invalidation

### Memory and Allocation

- **Command/Query Objects** - These are typically small, short-lived objects that are quickly garbage collected
- **Handler Registration** - Handlers are registered as scoped or transient services; choose appropriately based on your needs
- **Decorator Chain** - The decorator chain is built once per request and disposed after execution

### Database Performance

When using Entity Framework Core integration:

- **Use Projections** - Select only the data you need in queries
- **Avoid N+1 Queries** - Use `.Include()` or projections to load related data efficiently
- **Transaction Scope** - The transaction decorator uses `TransactionScope`; be aware of distributed transaction implications
- **Connection Pooling** - Entity Framework Core handles connection pooling; configure appropriately for your load

### Caching Best Practices

- **Cache Expensive Queries** - Use the caching decorator for queries that are expensive to compute
- **Set Appropriate TTL** - Balance freshness with performance; shorter TTL = more database hits
- **Cache Key Generation** - The framework generates cache keys based on query properties; ensure your queries are properly structured
- **Memory Limits** - Monitor memory usage when using in-memory caching for large result sets

### Monitoring and Profiling

- **Logging Decorator** - Use structured logging to track request duration and identify bottlenecks
- **Application Insights** - The logging decorator integrates with standard .NET logging; connect to Application Insights or similar tools
- **Custom Metrics** - Add custom decorators to track specific performance metrics
- **Cancellation Tokens** - Always pass and respect cancellation tokens to allow request cancellation

### Scalability

- **Stateless Design** - Handlers should be stateless; all state should be in the command/query or injected services
- **Horizontal Scaling** - The framework is designed for stateless, horizontally scalable applications
- **Async All the Way** - Use async/await throughout your handlers for better thread pool utilization
- **Background Processing** - For long-running operations, consider using background jobs instead of synchronous handlers

### Benchmarking

For high-performance scenarios, consider:

- **Measure First** - Profile your application to identify actual bottlenecks before optimizing
- **Decorator Overhead** - In most applications, decorator overhead is <1ms per request
- **Handler Performance** - Focus optimization efforts on handler logic, not the framework
- **Load Testing** - Test under realistic load to understand your application's performance characteristics

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
| [Minded.Framework.CQRS](https://www.nuget.org/packages/Minded.Framework.CQRS/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Framework.CQRS.svg) | CQRS implementation with base command and query classes |
| [Minded.Framework.Mediator.Abstractions](https://www.nuget.org/packages/Minded.Framework.Mediator.Abstractions/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Framework.Mediator.Abstractions.svg) | Mediator interfaces |
| [Minded.Framework.Mediator](https://www.nuget.org/packages/Minded.Framework.Mediator/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Framework.Mediator.svg) | Mediator implementation |
| [Minded.Framework.Decorator](https://www.nuget.org/packages/Minded.Framework.Decorator/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Framework.Decorator.svg) | Base classes for decorators |

### Extensions

| Package | Version | Description |
|---------|---------|-------------|
| [Minded.Extensions.Configuration](https://www.nuget.org/packages/Minded.Extensions.Configuration/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.Configuration.svg) | Configuration infrastructure and MindedBuilder for fluent decorator registration |
| [Minded.Extensions.WebApi](https://www.nuget.org/packages/Minded.Extensions.WebApi/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.WebApi.svg) | RestMediator for RESTful APIs |
| [Minded.Extensions.Validation.Abstractions](https://www.nuget.org/packages/Minded.Extensions.Validation.Abstractions/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.Validation.Abstractions.svg) | Validation interfaces and abstractions |
| [Minded.Extensions.Validation](https://www.nuget.org/packages/Minded.Extensions.Validation/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.Validation.svg) | Validation decorator with FluentValidation integration |
| [Minded.Extensions.Logging](https://www.nuget.org/packages/Minded.Extensions.Logging/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.Logging.svg) | Logging decorator with sensitive data protection |
| [Minded.Extensions.Exception](https://www.nuget.org/packages/Minded.Extensions.Exception/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.Exception.svg) | Exception handling decorator with sensitive data sanitization |
| [Minded.Extensions.Retry](https://www.nuget.org/packages/Minded.Extensions.Retry/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.Retry.svg) | Retry decorator for transient failure handling |
| [Minded.Extensions.Caching.Abstractions](https://www.nuget.org/packages/Minded.Extensions.Caching.Abstractions/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.Caching.Abstractions.svg) | Caching interfaces and abstractions |
| [Minded.Extensions.Caching.Memory](https://www.nuget.org/packages/Minded.Extensions.Caching.Memory/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.Caching.Memory.svg) | In-memory caching decorator implementation |
| [Minded.Extensions.Transaction](https://www.nuget.org/packages/Minded.Extensions.Transaction/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.Transaction.svg) | Transaction decorator with nested transaction support |
| [Minded.Extensions.DataProtection.Abstractions](https://www.nuget.org/packages/Minded.Extensions.DataProtection.Abstractions/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.DataProtection.Abstractions.svg) | Data protection and sanitization interfaces |
| [Minded.Extensions.DataProtection](https://www.nuget.org/packages/Minded.Extensions.DataProtection/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.DataProtection.svg) | Data protection implementation for PII/sensitive data sanitization |
| [Minded.Extensions.CQRS.EntityFrameworkCore](https://www.nuget.org/packages/Minded.Extensions.CQRS.EntityFrameworkCore/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.CQRS.EntityFrameworkCore.svg) | Entity Framework Core integration with base query classes |
| [Minded.Extensions.CQRS.OData](https://www.nuget.org/packages/Minded.Extensions.CQRS.OData/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.CQRS.OData.svg) | OData integration for CQRS queries |
| [Minded.Extensions.OData](https://www.nuget.org/packages/Minded.Extensions.OData/) | ![NuGet](https://img.shields.io/nuget/v/Minded.Extensions.OData.svg) | Core OData utilities and query composition support |

---

## Documentation

### Framework Documentation

- **[Changelog](Changelog.md)** - Version history and release notes

### Decorator Extensions Documentation

- **[Exception Handling](Extensions/Minded.Extensions.Exception/README.md)** - Centralized exception handling with sanitization pipeline
- **[Validation](Extensions/Minded.Extensions.Validation/README.md)** - FluentValidation integration for commands and queries
- **[Retry](Extensions/Minded.Extensions.Retry/README.md)** - Automatic retry logic for transient failures
- **[Logging](Extensions/Minded.Extensions.Logging/README.md)** - Comprehensive logging with sensitive data protection
- **[Caching](Extensions/Minded.Extensions.Caching.Memory/README.md)** - In-memory caching for query results
- **[Transaction](Extensions/Minded.Extensions.Transaction/README.md)** - Database transaction management with nested transaction support
- **[WebApi/RestMediator](Extensions/Minded.Extensions.WebApi/README.md)** - REST API integration with automatic HTTP response mapping
- **[Data Protection](Extensions/Minded.Extensions.DataProtection/README.md)** - Sensitive data protection for GDPR/CCPA compliance

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

**Made with ❤️ by [Manuel Salvatori](https://github.com/norcino)**
