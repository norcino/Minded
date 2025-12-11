# Minded.Extensions.Exception

Exception handling decorator with automatic error logging, graceful failure handling, and optional sensitive data protection.

## Features

- Automatic exception catching and logging
- OperationCanceledException special handling
- Configurable error responses
- Integration with Microsoft.Extensions.Logging
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
services.AddMinded(Configuration, assembly => assembly.Name.StartsWith("Service."), builder =>
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
services.AddMinded(Configuration, assembly => assembly.Name.StartsWith("Service."), builder =>
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
        // If this throws an exception, the decorator catches it
        var user = await _userRepository.CreateAsync(command.User);

        // Exception is logged with full context
        // Response is returned with Success = false
        return CommandResponse<User>.Success(user);
    }
}
```

### Exception Response

When an exception occurs:

```csharp
var command = new CreateUserCommand { User = new User { Name = "John" } };
var result = await _mediator.ProcessCommandAsync(command);

if (!result.Success)
{
    // result.Success = false
    // result.Outcomes contains error information
    // Exception details are logged but not exposed to caller
    Console.WriteLine($"Error: {result.Outcomes.First().Message}");
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
services.AddMinded(builder =>
{
    // Add exception handling for commands
    builder.AddCommandExceptionDecorator();

    // Add exception handling for queries
    builder.AddQueryExceptionDecorator();

    // Typically, you want both
});
```

### Decorator Order

The exception decorator should typically be registered **early** in the pipeline to catch exceptions from other decorators:

```csharp
services.AddMinded(builder =>
{
    builder.AddCommandExceptionDecorator();   // First - catches all exceptions
    builder.AddCommandLoggingDecorator();     // Second
    builder.AddCommandValidationDecorator();  // Third
    builder.AddCommandRetryDecorator();       // Fourth
    // Handler executes last
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
            return CommandResponse<PaymentResult>.Failure(
                "Payment was declined: " + ex.Reason);
        }
        catch (InsufficientFundsException ex)
        {
            // Different handling for different exceptions
            return CommandResponse<PaymentResult>.Failure(
                "Insufficient funds. Please use a different payment method.");
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
        return CommandResponse<User>.Failure("Error");
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
        return CommandResponse<Order>.Failure($"Product {ex.ProductId} is out of stock");
    }
    // Unknown exceptions bubble up to decorator
}
```

### 3. Use Cancellation Tokens

```csharp
// Good - pass cancellation token through
public async Task<CommandResponse<Data>> HandleAsync(
    GetDataQuery query,
    CancellationToken cancellationToken)
{
    var data = await _service.GetDataAsync(query.Id, cancellationToken);
    return QueryResponse<Data>.Success(data);
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

The following types are automatically excluded from exception logging:

- `CancellationToken`, `CancellationTokenSource`
- `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`
- `Func<>`, `Action<>`, and other delegate types
- `Stream`, `MemoryStream`, `FileStream`, and derived types
- `TextReader`, `TextWriter`, `StreamReader`, `StreamWriter`
- `Type`, `RuntimeTypeHandle`
- `IntPtr`, `UIntPtr`
- `IServiceProvider`
- ASP.NET Core HTTP types (`HttpContext`, etc.)

#### Explicit Exclusion with Attributes

For properties not automatically excluded, use one of these attributes:

**Option 1: `[ExcludeFromSerializedDiagnosticLogging]`** (Recommended for Minded)

```csharp
using Minded.Extensions.Exception;

public class ProcessFileCommand : ICommand
{
    public Guid TraceId { get; set; }
    public string FileName { get; set; }

    [ExcludeFromSerializedDiagnosticLogging]
    public byte[] FileContent { get; set; }  // Large binary data

    [ExcludeFromSerializedDiagnosticLogging]
    public IFormFile UploadedFile { get; set; }  // ASP.NET Core file
}
```

**Option 2: `[JsonIgnore]`** (Standard .NET attribute)

```csharp
using System.Text.Json.Serialization;

public class MyQuery : IQuery<Result>
{
    public Guid TraceId { get; set; }
    public int Id { get; set; }

    [JsonIgnore]
    public object InternalState { get; set; }  // Not for serialization
}
```

Both attributes work identically for exception logging exclusion. Use `[JsonIgnore]` if you also want the property excluded from API serialization, or `[ExcludeFromSerializedDiagnosticLogging]` if you only want to exclude from diagnostic logs.

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
