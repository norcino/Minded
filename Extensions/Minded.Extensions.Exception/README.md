# Minded.Extensions.Exception

Exception handling decorator with automatic error logging, graceful failure handling, and optional sensitive data protection.

## Features

- Automatic exception catching and logging
- OperationCanceledException special handling
- Configurable error responses
- Integration with Microsoft.Extensions.Logging
- **Centralized logging sanitization pipeline** - Automatically removes non-serializable types and sensitive data from exception logs
- **Extensible sanitization** - Add custom sanitizers to control what gets logged
- **Optional sensitive data protection** - Automatically hide PII and confidential data from exception logs (requires `Minded.Extensions.DataProtection`)

## Installation

```bash
dotnet add package Minded.Extensions.Exception
```

> **Note**: This package includes `Minded.Extensions.DataProtection` as a dependency, so you don't need to install it separately.

### Default Behavior (No Explicit Configuration)

If you don't explicitly configure DataProtection (i.e., you don't call `AddDataProtection()`), the exception decorator uses a **NullDataSanitizer** which:

- **Logs all properties** in exception context - including those marked with `[SensitiveData]`
- **Does NOT hide sensitive data** - all values are visible in exception logs
- **Works out-of-the-box** - no additional setup required

This is suitable for development environments or applications without sensitive data requirements.

## Usage

### Basic Usage (Without Data Protection)

```csharp
services.AddMinded(Configuration, assembly => assembly.Name.StartsWith("MindedExample.Application."), builder =>
{
    builder.AddCommandExceptionDecorator();
    builder.AddQueryExceptionDecorator();
});
```

### With Sensitive Data Protection

When DataProtection is explicitly configured, exception logs will automatically sanitize sensitive data.

> **📚 For complete DataProtection documentation**, see [Minded.Extensions.DataProtection README](../Minded.Extensions.DataProtection/README.md).

```csharp
using Minded.Extensions.DataProtection.Abstractions;

// Mark sensitive properties
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }

    [SensitiveData]  // Will be omitted from exception logs
    public string Email { get; set; }
}

// Configure DataProtection and Exception handling
services.AddMinded(Configuration, assembly => assembly.Name.StartsWith("MindedExample.Application."), builder =>
{
    builder.AddDataProtection(options =>
    {
        options.ShowSensitiveDataProvider = () => _environment.IsDevelopment();
    });

    builder.AddCommandExceptionDecorator();
    builder.AddQueryExceptionDecorator();
});
```

## How It Works

### Automatic Exception Handling

The exception decorator wraps your handler execution and catches any unhandled exceptions:

```csharp
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, User>
{
    public async Task<CommandResponse<User>> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        // If this throws an exception, the decorator catches it,
        // logs it with sanitized command context, and rethrows it
        // wrapped in a CommandHandlerException<TCommand>
        var user = await _userRepository.CreateAsync(command.User);

        return CommandResponse<User>.Success(user);
    }
}
```

### Exception Response

When an exception occurs, the decorator logs the original exception together with the sanitized command/query context and rethrows it wrapped in a `CommandHandlerException<TCommand>` (or `QueryHandlerException<TQuery, TResult>` for queries):

```csharp
var command = new CreateUserCommand { User = new User { Name = "John" } };

try
{
    var result = await _mediator.ProcessCommandAsync(command);
}
catch (CommandHandlerException<CreateUserCommand> ex)
{
    // The original exception is available as ex.InnerException
    // Sensitive data has already been sanitized in the logged context
    Console.WriteLine($"Error: {ex.Message}");
}
```

### Cancellation Handling

`OperationCanceledException` is handled specially and doesn't log as an error:

```csharp
public async Task<CommandResponse<Report>> HandleAsync(
    GenerateReportCommand command,
    CancellationToken cancellationToken)
{
    // If user cancels the request, this throws OperationCanceledException
    var data = await _reportService.GenerateAsync(command.ReportId, cancellationToken);

    // Exception decorator catches it and returns a cancelled response
    // Logged as Information, not Error
    return CommandResponse<Report>.Success(data);
}
```

## Configuration Options

### Decorator Registration

```csharp
services.AddMinded(configuration, mindedBuilderConfiguration: builder =>
{
    // Default registration (no configuration)
    builder.AddCommandExceptionDecorator();
    builder.AddQueryExceptionDecorator();

    // With configuration
    builder.AddCommandExceptionDecorator(options =>
    {
        options.Serialize = false;  // Disable serialization
    });

    builder.AddQueryExceptionDecorator(options =>
    {
        options.SerializeProvider = () => _environment.IsDevelopment();  // Dynamic
    });
});
```

### ExceptionOptions Class

Configure exception handling behavior:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Serialize` | `bool` | `true` | If `true`, serializes command/query to JSON in exception message. If `false`, only includes type name |
| `SerializeProvider` | `Func<bool>` | `null` | Dynamic provider for runtime configuration (takes precedence over `Serialize`). Useful for environment-based or feature flag control |

**Note:** Serialization can be expensive for large commands/queries. Consider disabling in production if you don't need full details in exception logs.

### Serialization Configuration

By default, the exception decorator serializes commands and queries when an exception occurs. This can be expensive for large objects or unnecessary in production environments. You can configure serialization behavior:

#### Disable Serialization

```csharp
services.AddMinded(configuration, mindedBuilderConfiguration: builder =>
{
    // Disable serialization for commands
    builder.AddCommandExceptionDecorator(options =>
    {
        options.Serialize = false;
    });

    // Disable serialization for queries
    builder.AddQueryExceptionDecorator(options =>
    {
        options.Serialize = false;
    });
});
```

When serialization is disabled, only the command/query type name is included in the exception message:

```text
Type: CreateUserCommand (serialization disabled)
```

#### Dynamic Serialization (Environment-Based)

Use a provider function for runtime configuration:

```csharp
services.AddMinded(configuration, mindedBuilderConfiguration: builder =>
{
    // Serialize only in development
    builder.AddCommandExceptionDecorator(options =>
    {
        options.SerializeProvider = () => _environment.IsDevelopment();
    });

    builder.AddQueryExceptionDecorator(options =>
    {
        options.SerializeProvider = () => _environment.IsDevelopment();
    });
});
```

#### Feature Flag-Based Serialization

```csharp
services.AddMinded(configuration, mindedBuilderConfiguration: builder =>
{
    builder.AddCommandExceptionDecorator(options =>
    {
        options.SerializeProvider = () => _featureFlags.IsEnabled("DetailedExceptionLogging");
    });
});
```

**Note:** The `SerializeProvider` function takes precedence over the `Serialize` property when both are set.

### Decorator Order

Decorator registration follows this rule: **first registered = innermost (runs last, right before the handler); last registered = outermost (runs first)**. The exception decorator must be registered **last**, so it is the outermost decorator and catches exceptions thrown by all other decorators and the handler:

```csharp
services.AddMinded(configuration, mindedBuilderConfiguration: builder =>
{
    builder.AddCommandValidationDecorator();  // First registered = innermost, runs right before the handler
    builder.AddCommandRetryDecorator();
    builder.AddCommandLoggingDecorator();
    builder.AddCommandExceptionDecorator();   // Last registered = outermost, catches everything
    builder.AddCommandHandlers();             // Actual handlers - always innermost regardless of position
});
```

## Advanced Usage

### Custom Exception Handling

For custom exception handling, catch exceptions in your handler and return appropriate responses:

```csharp
public class ProcessPaymentCommandHandler : ICommandHandler<ProcessPaymentCommand, PaymentResult>
{
    public async Task<CommandResponse<PaymentResult>> HandleAsync(
        ProcessPaymentCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _paymentGateway.ProcessAsync(command.Amount, command.CardToken);
            return CommandResponse<PaymentResult>.Success(result);
        }
        catch (PaymentDeclinedException ex)
        {
            // Handle specific exception with custom message
            return CommandResponse<PaymentResult>.Error(
                new OutcomeEntry(null, "Payment was declined: " + ex.Reason));
        }
        catch (InsufficientFundsException ex)
        {
            // Different handling for different exceptions
            return CommandResponse<PaymentResult>.Error(
                new OutcomeEntry(null, "Insufficient funds. Please use a different payment method."));
        }
        // Other exceptions bubble up to the exception decorator
    }
}
```

### Exception Logging Context

The exception decorator logs exceptions with full context including:

- Command/Query type and properties
- Exception type, message, and stack trace
- Timestamp and correlation ID
- Sensitive data is automatically sanitized if DataProtection is configured

```csharp
// Log output example:
// Error executing CreateUserCommand
// Exception: SqlException: Cannot insert duplicate key
// Command: { "User": { "Id": 123, "Name": "John" } }
// Email and other [SensitiveData] properties are omitted
```

## Centralized Logging Sanitization Pipeline

The exception decorator uses a **centralized logging sanitization pipeline** (`ILoggingSanitizerPipeline`) that processes commands and queries before logging them. This pipeline:

1. **Converts objects to dictionaries** - Recursively inspects objects and converts them to key-value pairs
2. **Removes non-serializable types** - Automatically excludes types that cannot be serialized (see [Automatically Excluded Types](#automatically-excluded-types))
3. **Applies registered sanitizers** - Runs all registered `ILoggingSanitizer` implementations in order
4. **Excludes interface properties** - Removes properties added by specific interfaces (e.g., `ILoggable`)

### How the Pipeline Works

```csharp
// The exception decorator uses the pipeline to sanitize commands/queries before logging
public class ExceptionCommandHandlerDecorator<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private readonly ILoggingSanitizerPipeline _sanitizerPipeline;

    public async Task<CommandResponse> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        try
        {
            return await _innerHandler.HandleAsync(command, cancellationToken);
        }
        catch (Exception ex)
        {
            // Sanitize the command before logging
            var sanitizedCommand = _sanitizerPipeline.Sanitize(command);
            var commandJson = JsonSerializer.Serialize(sanitizedCommand);

            _logger.LogError(ex, "Error executing {CommandType}: {Command}",
                typeof(TCommand).Name, commandJson);

            throw new CommandHandlerException(ex.Message, ex);
        }
    }
}
```

### Built-in Sanitizers

The framework includes the following built-in sanitizers:

#### 1. DataProtectionLoggingSanitizer

Automatically registered when you call `AddDataProtection()`. This sanitizer:
- Removes properties/fields marked with `[SensitiveData]` attribute
- Respects `ShowSensitiveData` configuration option
- Works with both properties and fields

```csharp
builder.AddDataProtection(options =>
{
    options.ShowSensitiveDataProvider = () => _environment.IsDevelopment();
});
```

### Creating Custom Sanitizers

You can create custom sanitizers to control what gets logged. Implement the `ILoggingSanitizer` interface:

```csharp
using Minded.Framework.CQRS.Abstractions.Sanitization;

public class MyCustomSanitizer : ILoggingSanitizer
{
    public IDictionary<string, object> Sanitize(IDictionary<string, object> data, Type sourceType)
    {
        // Example: Remove all properties containing "Internal" in the name
        var sanitized = new Dictionary<string, object>();

        foreach (var kvp in data)
        {
            if (!kvp.Key.Contains("Internal"))
            {
                sanitized[kvp.Key] = kvp.Value;
            }
        }

        return sanitized;
    }
}
```

Register your custom sanitizer as a singleton:

```csharp
services.AddMinded(Configuration, assembly => assembly.Name.StartsWith("MindedExample.Application."), builder =>
{
    // Register your custom sanitizer
    builder.ServiceCollection.AddSingleton<ILoggingSanitizer, MyCustomSanitizer>();

    // The pipeline will automatically discover and use it
    builder.AddCommandExceptionDecorator();
    builder.AddQueryExceptionDecorator();
});
```

### Excluding Interface Properties

Some decorators automatically exclude properties added by specific interfaces. For example, the logging decorator excludes `LoggingTemplate` and `LoggingProperties` from `ILoggable`:

```csharp
// This is done automatically by the logging decorator
builder.RegisterLoggingSanitizerPipelineConfiguration(pipeline =>
{
    pipeline.ExcludeProperties(typeof(ILoggable), "LoggingTemplate", "LoggingProperties");
});
```

You can exclude properties from your own interfaces in custom decorators:

```csharp
public static MindedBuilder AddMyCustomDecorator(this MindedBuilder builder)
{
    // Exclude properties from IMyInterface
    builder.RegisterLoggingSanitizerPipelineConfiguration(pipeline =>
    {
        pipeline.ExcludeProperties(typeof(IMyInterface), "InternalProperty1", "InternalProperty2");
    });

    // Register the decorator
    builder.QueueCommandDecoratorRegistrationAction((b, i) =>
        b.DecorateHandlerDescriptors(i, typeof(MyCustomDecorator<>)));

    return builder;
}
```

## Best Practices

### 1. Let the Decorator Handle Unexpected Exceptions

```csharp
// Good - let decorator handle unexpected exceptions
public async Task<CommandResponse<User>> HandleAsync(...)
{
    var user = await _userRepository.CreateAsync(command.User);
    return CommandResponse<User>.Success(user);
}

// Avoid - catching all exceptions hides problems
public async Task<CommandResponse<User>> HandleAsync(...)
{
    try
    {
        var user = await _userRepository.CreateAsync(command.User);
        return CommandResponse<User>.Success(user);
    }
    catch (Exception ex)
    {
        // Don't do this - let the decorator handle it
        return CommandResponse<User>.Error(new OutcomeEntry(null, "Error"));
    }
}
```

### 2. Handle Known Exceptions Explicitly

```csharp
// Good - handle known business exceptions
public async Task<CommandResponse<Order>> HandleAsync(...)
{
    try
    {
        return await ProcessOrderAsync(command);
    }
    catch (OutOfStockException ex)
    {
        // Known business exception - handle explicitly
        return CommandResponse<Order>.Error(
            new OutcomeEntry(null, $"Product {ex.ProductId} is out of stock"));
    }
    // Unknown exceptions bubble up to decorator
}
```

### 3. Use Cancellation Tokens

```csharp
// Good - pass cancellation token through
public async Task<Data> HandleAsync(
    GetDataQuery query,
    CancellationToken cancellationToken)
{
    return await _service.GetDataAsync(query.Id, cancellationToken);
}
```

### 4. Mark Sensitive Data

Always mark sensitive properties to prevent them from appearing in exception logs:

```csharp
public class PaymentCommand : ICommand<PaymentResult>
{
    public decimal Amount { get; set; }

    [SensitiveData]
    public string CardNumber { get; set; }

    [SensitiveData]
    public string CVV { get; set; }
}
```

### 5. Exclude Non-Serializable Properties

Some properties cannot be serialized and will cause issues in exception logs. The exception decorator automatically excludes common non-serializable types, but you can also explicitly exclude properties.

#### Automatically Excluded Types

The exception decorators sanitize the command/query through the centralized `ILoggingSanitizerPipeline` (implemented by `LoggingSanitizerPipeline` in `Minded.Framework.CQRS`). The pipeline automatically excludes the following non-serializable property types:

- `CancellationToken`
- `Task` and `Task<T>`
- `Stream` (the exact `System.IO.Stream` type)
- All delegate types (`Func<>`, `Action<>`, custom delegates)

Property types not covered by this built-in list — e.g. `CancellationTokenSource`, derived stream types, `IServiceProvider` — are not skipped automatically: exclude them explicitly via `ExcludeProperties` or a custom `ILoggingSanitizer` (see below). See `LoggingSanitizerPipeline.cs` in `Minded.Framework.CQRS` for the authoritative list.

> **Note:** This package also ships a standalone `DiagnosticDataSanitizer` static helper with a broader exclusion list, but it is **not** used by the exception decorators at runtime.

#### Explicitly Excluding Properties

For properties not automatically excluded, use one of these mechanisms:

**Option 1: Exclude interface-declared properties via the pipeline**

If the properties to exclude are declared on an interface, register a pipeline configuration with `pipeline.ExcludeProperties(...)` — see [Excluding Interface Properties](#excluding-interface-properties) above. This is how the logging decorator excludes the `ILoggable` members.

**Option 2: Custom sanitizer**

For arbitrary properties, implement a custom `ILoggingSanitizer` that removes the entries from the sanitized dictionary — see [Creating Custom Sanitizers](#creating-custom-sanitizers) above.

> **Note:** Earlier versions provided an `[ExcludeFromSerializedDiagnosticLogging]` attribute; it has been removed. Property exclusion is now handled by the sanitization pipeline (`ExcludeProperties` or custom `ILoggingSanitizer` implementations). `[JsonIgnore]` is **not** honored by the sanitization pipeline, because the command/query is converted to a dictionary before JSON serialization takes place.

## Integration with RestMediator

When using with `Minded.Extensions.WebApi`, exceptions automatically return appropriate HTTP status codes:

```csharp
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IRestMediator _mediator;

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
    {
        // Exceptions are caught by the decorator
        // Returns 500 Internal Server Error with sanitized error message
        return await _mediator.ProcessCommandAsync(command);
    }
}
```

## Troubleshooting

### Exceptions Not Being Caught

Ensure the exception decorator is registered:

```csharp
builder.AddCommandExceptionDecorator();
builder.AddQueryExceptionDecorator();
```

### Sensitive Data Appearing in Logs

If sensitive data (properties marked with `[SensitiveData]`) appears in your exception logs:

1. **Ensure DataProtection is explicitly configured** - without calling `AddDataProtection()`, all data is logged:

```csharp
builder.AddDataProtection();
```

2. **Mark sensitive properties** with the `[SensitiveData]` attribute:

```csharp
[SensitiveData]
public string Email { get; set; }
```

3. **Ensure `ShowSensitiveData = false`** in production:

```csharp
builder.AddDataProtection(options =>
{
    options.ShowSensitiveData = false;  // Hide sensitive data
});
```

> **📚 For more details**, see [Minded.Extensions.DataProtection README](../Minded.Extensions.DataProtection/README.md).

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.Exception)
- [DataProtection Documentation](../Minded.Extensions.DataProtection/README.md)
- [Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
