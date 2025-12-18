# Minded.Extensions.Logging

Logging decorator for automatic command and query execution logging with configurable detail levels, outcome tracking, custom templates, and optional sensitive data protection (requires `Minded.Extensions.DataProtection`).

## Features

- **Automatic logging** for all command and query executions (start, complete, fail)
- **TraceId correlation** for request tracking across distributed systems
- **Execution timing** for performance monitoring
- **Outcome entry logging** with severity filtering (errors, warnings, info)
- **Custom log templates** via the `ILoggable` interface
- **Sensitive data protection** integration (requires `Minded.Extensions.DataProtection`)
- **Dynamic configuration** via providers (feature flags support)

## Installation

```bash
dotnet add package Minded.Extensions.Logging
```

> **Note**: This package includes `Minded.Extensions.DataProtection` as a dependency, so you don't need to install it separately.

### Default Behavior (No Explicit Configuration)

If you don't explicitly configure DataProtection (i.e., you don't call `AddDataProtection()`), the logging decorator uses a **NullDataSanitizer** which:

- **Logs all properties** - including those marked with `[SensitiveData]`
- **Does NOT hide sensitive data** - all values are visible in logs
- **Works out-of-the-box** - no additional setup required

This is suitable for development environments or applications without sensitive data requirements.

### To Enable Sensitive Data Protection

If you want to hide properties marked with `[SensitiveData]`, you must explicitly configure DataProtection:

```csharp
builder.AddDataProtection(options =>
{
    options.ShowSensitiveData = false; // Hide sensitive data (production)
    // Or dynamically:
    // options.ShowSensitiveDataProvider = () => _environment.IsDevelopment();
});

builder.AddCommandLoggingDecorator();
builder.AddQueryLoggingDecorator();
```

See [Sensitive Data Protection](#sensitive-data-protection) for more details.

## Quick Start

### Registration

Register the logging decorators in your `Program.cs` or `Startup.cs`:

```csharp
services.AddMinded(Configuration, assembly => assembly.Name.StartsWith("Service."), builder =>
{
    // Add command logging decorator
    builder.AddCommandLoggingDecorator();

    // Add query logging decorator
    builder.AddQueryLoggingDecorator();
});
```

### What Gets Logged Automatically

Once registered, **all commands and queries are automatically logged**. No additional interfaces are required for basic logging.

#### Example Log Output

Commands:

```text
[Tracking:a1b2c3d4-e5f6-7890-abcd-ef1234567890] CreateUserCommand - Started
[Tracking:a1b2c3d4-e5f6-7890-abcd-ef1234567890] CreateUserCommand in 00:00:00.0234567 - Completed: True
```

Queries:

```text
[Tracking:b2c3d4e5-f6a7-8901-bcde-f12345678901] GetUserByIdQuery - Started
[Tracking:b2c3d4e5-f6a7-8901-bcde-f12345678901] GetUserByIdQuery in 00:00:00.0123456 - Completed
```

On exception:

```text
[Tracking:c3d4e5f6-a7b8-9012-cdef-123456789012] CreateUserCommand in 00:00:00.0345678 - Failed: User already exists
```

## Configuration Options

### Complete LoggingOptions Reference

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | `bool` | `false` | Enable/disable logging |
| `EnabledProvider` | `Func<bool>` | `null` | Dynamic enable/disable (takes precedence over `Enabled`) |
| `LogMessageTemplateData` | `bool` | `false` | Include custom template data from `ILoggable` interface |
| `LogMessageTemplateDataProvider` | `Func<bool>` | `null` | Dynamic template data toggle |
| `LogOutcomeEntries` | `bool` | `false` | Enable/disable Log outcome entries from command/query responses |
| `LogOutcomeEntriesProvider` | `Func<bool>` | `null` | Dynamic outcome logging toggle |
| `MinimumOutcomeSeverityLevel` | `Severity` | `Info` | Minimum outcome entries severity to log: `Error`, `Warning`, or `Info` |
| `MinimumOutcomeSeverityLevelProvider` | `Func<Severity>` | `null` | Dynamic severity filter for outcome entries|

### Configuration via appsettings.json

The parameterless registration methods read from `Minded:LoggingOptions`:

```json
{
  "Minded": {
    "LoggingOptions": {
      "Enabled": true,
      "LogMessageTemplateData": true,
      "LogOutcomeEntries": true,
      "MinimumOutcomeSeverityLevel": "Warning"
    }
  }
}
```

### Programmatic Configuration

```csharp
builder.AddCommandLoggingDecorator(options =>
{
    options.Enabled = true;
    options.LogMessageTemplateData = true;
    options.LogOutcomeEntries = true;
    options.MinimumOutcomeSeverityLevel = Severity.Warning;
});

builder.AddQueryLoggingDecorator(options =>
{
    options.Enabled = true;
    options.LogMessageTemplateData = true;
});
```

### Dynamic Configuration with Providers

Use providers for runtime configuration based on feature flags, environment, or configuration:

```csharp
builder.AddCommandLoggingDecorator(options =>
{
    // Enable logging based on environment
    options.EnabledProvider = () => !_environment.IsProduction();

    // Log outcome entries based on feature flag
    options.LogOutcomeEntriesProvider = () => _featureFlags.IsEnabled("DetailedLogging");

    // Adjust severity based on configuration
    options.MinimumOutcomeSeverityLevelProvider = () =>
        _configuration.GetValue<Severity>("Logging:MinSeverity", Severity.Warning);
});
```

## Custom Logging with ILoggable Interface

The `ILoggable` interface is **optional**. Implement it when you want to add custom contextual data to your logs.

### ILoggable Interface

```csharp
public interface ILoggable
{
    /// <summary>
    /// Template to be used for string interpolation (Serilog-style placeholders)
    /// </summary>
    string LoggingTemplate { get; }

    /// <summary>
    /// Property paths to extract from the command/query for logging.
    /// Supports dot notation for nested properties (e.g., "User.Email", "Order.Customer.Name").
    /// Properties marked with [SensitiveData] are automatically masked based on DataProtection configuration.
    /// All logging data must go through this property to ensure proper sanitization.
    /// </summary>
    string[] LoggingProperties { get; }
}
```

### Example: Command with Property Paths

```csharp
using Minded.Extensions.Logging;
using Minded.Extensions.DataProtection.Abstractions;
using Minded.Framework.CQRS.Command;

public class User
{
    [SensitiveData]
    public string Email { get; set; }

    [SensitiveData]
    public string Name { get; set; }

    [SensitiveData]
    public string Surname { get; set; }
}

public class CreateUserCommand : ICommand<User>, ILoggable
{
    public User User { get; set; }
    public Guid TraceId { get; } = Guid.NewGuid();

    // Custom logging template - uses Serilog-style placeholders
    public string LoggingTemplate => "Creating user with email: {Email} - name: {Name}, surname: {Surname}";

    // Property paths - framework navigates from "this" and checks [SensitiveData] attributes
    public string[] LoggingProperties => new[] { "User.Email", "User.Name", "User.Surname" };
}
```

**Log Output** (with `ShowSensitiveData = false`):
```text
[Tracking:a1b2c3d4-...] CreateUserCommand - Creating user with email: ***MASKED*** - name: ***MASKED***, surname: ***MASKED*** - Started
```

**Log Output** (with `ShowSensitiveData = true`):
```text
[Tracking:a1b2c3d4-...] CreateUserCommand - Creating user with email: john@example.com - name: John, surname: Doe - Started
```

### Property Path Navigation

Property paths support dot notation to navigate nested objects:

```csharp
public class UpdateOrderCommand : ICommand, ILoggable
{
    public int OrderId { get; set; }
    public Customer Customer { get; set; }
    public Category Category { get; set; }

    public string LoggingTemplate => "Order {OrderId} - Customer: {Email}/{Name} - Category: {CategoryName}";

    // Mix root-level properties with nested navigation
    public string[] LoggingProperties => new[]
    {
        nameof(OrderId),           // this.OrderId
        "Customer.Email",          // this.Customer.Email (masked if [SensitiveData])
        "Customer.Name",           // this.Customer.Name (masked if [SensitiveData])
        "Category.Name"            // this.Category.Name
    };
}
```

**Features:**
- ✅ Supports nested navigation: `"Order.Customer.Address.City"`
- ✅ Supports both properties and fields
- ✅ Automatic sanitization based on `[SensitiveData]` attributes at any level
- ✅ `nameof()` support for root-level properties (compile-time safety)
- ✅ Null-safe: returns `null` if any part of the path is null
- ✅ Performance optimized with reflection caching

### Log Output with ILoggable

When `LogMessageTemplateData = true`, the custom template is appended to the standard log:

```text
[Tracking:a1b2c3d4-...] CreateTransactionCommand Credit: 100.00 Debit: 0.00 CategoryId: 5 - Started
[Tracking:a1b2c3d4-...] CreateTransactionCommand Credit: 100.00 Debit: 0.00 CategoryId: 5 in 00:00:00.0234567 - Completed: True
```

## Outcome Entries Logging

### What Are Outcome Entries?

Outcome entries are structured messages attached to command/query responses. They represent:

- **Validation errors** (e.g., "Name is required")
- **Business rule violations** (e.g., "Insufficient funds")
- **Warnings** (e.g., "Near credit limit")
- **Informational messages** (e.g., "Created new customer record")

### Where Do They Come From?

Outcome entries are part of `ICommandResponse.OutcomeEntries` or `IQueryResponse.OutcomeEntries`:

```csharp
public interface IMessageResponse
{
    bool Successful { get; set; }
    List<IOutcomeEntry> OutcomeEntries { get; set; }
}

public interface IOutcomeEntry
{
    string PropertyName { get; }      // Property that caused the issue
    string Message { get; }           // Human-readable message
    string ErrorCode { get; set; }    // Error code for localization
    Severity Severity { get; set; }   // Error, Warning, or Info
    object AttemptedValue { get; }    // The value that failed
}
```

### Enabling Outcome Entry Logging

```csharp
builder.AddCommandLoggingDecorator(options =>
{
    options.Enabled = true;
    options.LogOutcomeEntries = true;
    options.MinimumOutcomeSeverityLevel = Severity.Warning; // Only Warning and Error
});
```

### Outcome Entry Log Output

```text
[Tracking:a1b2c3d4-...] CreateUserCommand - Outcome: [Error] Email is required (Property: Email, Code: NotEmpty)
[Tracking:a1b2c3d4-...] CreateUserCommand - Outcome: [Warning] Password is weak (Property: Password, Code: WeakPassword)
```

### Severity Levels and Log Levels

| Outcome Severity | .NET Log Level |
|------------------|----------------|
| `Severity.Error` (0) | `LogLevel.Error` |
| `Severity.Warning` (1) | `LogLevel.Warning` |
| `Severity.Info` (2) | `LogLevel.Information` |

The `MinimumOutcomeSeverityLevel` filter works as follows:

- `Severity.Error` (0) → Only log Error entries
- `Severity.Warning` (1) → Log Warning and Error entries
- `Severity.Info` (2) → Log all entries (default)

## TraceId Correlation

Every command and query has a `TraceId` property (from `IMessage` interface) for request correlation:

```csharp
public interface IMessage
{
    /// <summary>
    /// Tracing Id used to track all commands and queries coming from the same request
    /// </summary>
    Guid TraceId { get; }
}
```

### Passing TraceId Across Operations

```csharp
// In a controller or handler, pass the same TraceId to correlate operations
var traceId = Guid.NewGuid();

var userQuery = new GetUserByIdQuery(userId, traceId);
var user = await _mediator.Send(userQuery);

var createOrderCommand = new CreateOrderCommand(order, traceId);
await _mediator.Send(createOrderCommand);

// Both operations will have the same TraceId in logs
```

## Sensitive Data Protection

For GDPR/CCPA compliance, you can configure DataProtection to hide properties marked with `[SensitiveData]`.

> **📚 For complete DataProtection documentation**, see [Minded.Extensions.DataProtection README](../Minded.Extensions.DataProtection/README.md).

### 1. Mark Sensitive Properties

```csharp
using Minded.Extensions.DataProtection.Abstractions;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }

    [SensitiveData]
    public string Email { get; set; }

    [SensitiveData]
    public string CreditCardNumber { get; set; }
}
```

### 2. Configure DataProtection

```csharp
builder.AddDataProtection(options =>
{
    // Hide sensitive data by default
    options.ShowSensitiveData = false;

    // Or dynamic: show only in development
    options.ShowSensitiveDataProvider = () => _environment.IsDevelopment();
});

builder.AddCommandLoggingDecorator();
```

### 3. Automatic Protection

When logged, sensitive properties are automatically omitted:

```json
// Without [SensitiveData]:
{ "Id": 123, "Name": "John", "Email": "john@example.com" }

// With [SensitiveData] (ShowSensitiveData = false):
{ "Id": 123, "Name": "John" }
```

## Complete Example

```csharp
// Program.cs
services.AddMinded(Configuration, asm => asm.Name.StartsWith("Service."), builder =>
{
    // Enable data protection for sensitive data handling
    builder.AddDataProtection(options =>
    {
        options.ShowSensitiveDataProvider = () => _environment.IsDevelopment();
    });

    // Enable command logging with full configuration
    builder.AddCommandLoggingDecorator(options =>
    {
        options.Enabled = true;
        options.LogMessageTemplateData = true;    // Include ILoggable data
        options.LogOutcomeEntries = true;         // Log validation errors, etc.
        options.MinimumOutcomeSeverityLevel = Severity.Warning;
    });

    // Enable query logging
    builder.AddQueryLoggingDecorator(options =>
    {
        options.Enabled = true;
        options.LogMessageTemplateData = true;
    });
});

// Command with custom logging
public class CreateOrderCommand : ICommand<Order>, ILoggable
{
    public Order Order { get; set; }
    public Guid TraceId { get; } = Guid.NewGuid();

    public string LoggingTemplate => "CustomerId: {CustomerId} Total: {Total}";
    public string[] LoggingProperties => new[] { "Order.CustomerId", "Order.Total" };
}
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.Logging)
- [DataProtection Documentation](../Minded.Extensions.DataProtection/README.md)
- [Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
