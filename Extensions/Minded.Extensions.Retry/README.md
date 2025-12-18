# Minded.Extensions.Retry

Retry decorator for handling transient failures with configurable retry policies, exponential backoff, and detailed logging.

## Features

- **Configurable Retry Policies** - Set retry count and delays per command/query
- **Exponential Backoff** - Automatically increase delay between retries
- **Transient Failure Handling** - Retry only on specific exception types
- **Detailed Logging** - Track retry attempts and failures
- **Cancellation Support** - Respect cancellation tokens during retries
- **Per-Operation Configuration** - Different retry policies for different operations

## Installation

```bash
dotnet add package Minded.Extensions.Retry
```

## Quick Start

### 1. Create a Command with Retry Configuration

```csharp
using Minded.Framework.CQRS.Abstractions;
using Minded.Extensions.Retry.Decorator;

[RetryCommand(3, 1000, 2000, 4000)]  // 3 retries with exponential backoff
public class SendEmailCommand : ICommand<bool>
{
    public string To { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
}
```

### 2. Configure Retry Decorator

```csharp
using Minded.Extensions.Configuration;

services.AddMinded(builder =>
{
    // Add retry decorator for commands
    builder.AddCommandRetryDecorator();

    // Add retry decorator for queries (optional)
    builder.AddQueryRetryDecorator();
});
```

### 3. Automatic Retry on Failure

```csharp
var command = new SendEmailCommand
{
    To = "user@example.com",
    Subject = "Welcome",
    Body = "Welcome to our service!"
};

// If the handler throws a transient exception (e.g., network timeout),
// it will automatically retry up to 3 times with exponential backoff:
// - Attempt 1: Immediate
// - Attempt 2: Wait 1 second
// - Attempt 3: Wait 2 seconds
// - Attempt 4: Wait 4 seconds
var result = await _mediator.ProcessCommandAsync(command);
```

## Configuration Options

### Decorator Registration

The retry decorator supports optional configuration for default retry behavior:

```csharp
// Default registration (uses defaults: 3 retries, no delays)
builder.AddCommandRetryDecorator();
builder.AddQueryRetryDecorator();

// With default retry configuration
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

### RetryOptions Class

Configure default retry behavior when attributes don't specify values:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DefaultRetryCount` | `int` | `3` | Default number of retry attempts when not specified in attribute |
| `DefaultRetryCountProvider` | `Func<int>` | `null` | Dynamic provider for retry count (takes precedence over static value) |
| `DefaultDelay1` | `int` | `0` | Default delay in milliseconds before first retry |
| `DefaultDelay1Provider` | `Func<int>` | `null` | Dynamic provider for first retry delay (takes precedence over static value) |
| `DefaultDelay2` | `int` | `0` | Default delay in milliseconds before second retry |
| `DefaultDelay2Provider` | `Func<int>` | `null` | Dynamic provider for second retry delay (takes precedence over static value) |
| `DefaultDelay3` | `int` | `0` | Default delay in milliseconds before third retry |
| `DefaultDelay3Provider` | `Func<int>` | `null` | Dynamic provider for third retry delay (takes precedence over static value) |
| `DefaultDelay4` | `int` | `0` | Default delay in milliseconds before fourth retry |
| `DefaultDelay4Provider` | `Func<int>` | `null` | Dynamic provider for fourth retry delay (takes precedence over static value) |
| `DefaultDelay5` | `int` | `0` | Default delay in milliseconds before fifth retry |
| `DefaultDelay5Provider` | `Func<int>` | `null` | Dynamic provider for fifth retry delay (takes precedence over static value) |
| `ApplyToAllQueries` | `bool` | `false` | If `true`, applies retry logic to ALL queries even without `[RetryQuery]` attribute |
| `ApplyToAllQueriesProvider` | `Func<bool>` | `null` | Dynamic provider for apply to all queries (takes precedence over static value) |

### Configuration via appsettings.json

```json
{
  "Minded": {
    "RetryOptions": {
      "DefaultRetryCount": 3,
      "DefaultDelay1": 100,
      "DefaultDelay2": 200,
      "DefaultDelay3": 400,
      "ApplyToAllQueries": false
    }
  }
}
```

### Runtime Configuration with Providers

All `RetryOptions` properties support dynamic runtime configuration via provider functions. This allows you to change retry behavior at runtime based on feature flags, configuration services, or other runtime conditions.

**Example: Runtime Configuration with Feature Flags**

```csharp
builder.AddCommandRetryDecorator(options =>
{
    // Static configuration (fallback values)
    options.DefaultRetryCount = 3;
    options.DefaultDelay1 = 100;

    // Dynamic configuration via providers (takes precedence)
    options.DefaultRetryCountProvider = () => _configService.GetValue("retry-count", 3);
    options.DefaultDelay1Provider = () => _configService.GetValue("retry-delay1", 100);
    options.DefaultDelay2Provider = () => _configService.GetValue("retry-delay2", 200);
    options.ApplyToAllQueriesProvider = () => _featureFlags.IsEnabled("retry-all-queries");
});
```

**Example: Environment-Based Configuration**

```csharp
builder.AddQueryRetryDecorator(applyToAllQueries: false, configureOptions: options =>
{
    // More aggressive retries in production
    options.DefaultRetryCountProvider = () => _environment.IsProduction() ? 5 : 2;
    options.DefaultDelay1Provider = () => _environment.IsProduction() ? 200 : 50;
});
```

**How Providers Work:**

- Providers are invoked **each time** a retry operation is initiated
- If a provider is set, it takes precedence over the static property value
- If a provider returns `null` or is not set, the static property value is used
- This enables true runtime configuration without application restart

**GetEffective Methods:**

The `RetryOptions` class provides `GetEffective*()` methods that handle the provider logic:

```csharp
public int GetEffectiveDefaultRetryCount()
{
    return DefaultRetryCountProvider?.Invoke() ?? DefaultRetryCount;
}

public bool GetEffectiveApplyToAllQueries()
{
    return ApplyToAllQueriesProvider?.Invoke() ?? ApplyToAllQueries;
}
// ... similar methods for all delay properties
```

### Retry Attributes

Retry behavior is configured per command/query using attributes:

**[RetryCommand] Attribute:**

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class RetryCommandAttribute : Attribute
{
    public int? RetryCount { get; }
    public int? Delay1 { get; }  // Delay in milliseconds
    public int? Delay2 { get; }
    public int? Delay3 { get; }
    public int? Delay4 { get; }
    public int? Delay5 { get; }
}
```

**[RetryQuery] Attribute:**

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class RetryQueryAttribute : Attribute
{
    public int? RetryCount { get; }
    public int? Delay1 { get; }  // Delay in milliseconds
    public int? Delay2 { get; }
    public int? Delay3 { get; }
    public int? Delay4 { get; }
    public int? Delay5 { get; }
}
```

**Attribute Constructors:**

```csharp
// Default - uses RetryOptions defaults
[RetryCommand]

// Specify retry count only
[RetryCommand(3)]

// Specify retry count and single delay (used for all retries)
[RetryCommand(3, 1000)]  // 3 retries, 1 second delay each

// Specify retry count and multiple delays (exponential backoff)
[RetryCommand(3, 100, 200, 400)]  // 3 retries: 100ms, 200ms, 400ms
```

### Example Configurations

#### Aggressive Retry (Fast, Many Attempts)

```csharp
using Minded.Extensions.Retry.Decorator;

[RetryQuery(5, 100, 200, 400, 800, 1600)]  // 5 retries with exponential backoff
public class CheckInventoryQuery : IQuery<InventoryStatus>
{
    public int ProductId { get; set; }
}

// Retry schedule: 100ms, 200ms, 400ms, 800ms, 1600ms
```

#### Conservative Retry (Slow, Few Attempts)

```csharp
using Minded.Extensions.Retry.Decorator;

[RetryCommand(2, 5000)]  // 2 retries with 5 second delay each
public class ProcessPaymentCommand : ICommand<PaymentResult>
{
    public decimal Amount { get; set; }
    public string CardToken { get; set; }
}

// Retry schedule: 5s, 5s (fixed delay)
```

#### Default Retry Configuration

```csharp
using Minded.Extensions.Retry.Decorator;

[RetryCommand]  // Uses defaults from RetryOptions (typically 3 retries)
public class SendNotificationCommand : ICommand<bool>
{
    public string Message { get; set; }
}
```

#### No Attribute = No Retry

```csharp
// No [RetryCommand] attribute = no retry logic applied
public class DeleteUserCommand : ICommand<bool>
{
    public int UserId { get; set; }
}
```

## Advanced Usage

### Retry with Custom Exception Handling

The retry decorator will retry on any exception by default. To retry only on specific exceptions, implement custom logic in your handler:

```csharp
public class SendEmailCommandHandler : ICommandHandler<SendEmailCommand, bool>
{
    private readonly IEmailService _emailService;

    public SendEmailCommandHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<CommandResponse<bool>> HandleAsync(
        SendEmailCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            await _emailService.SendAsync(command.To, command.Subject, command.Body);
            return CommandResponse<bool>.Success(true);
        }
        catch (SmtpException ex) when (IsTransient(ex))
        {
            // Throw to trigger retry
            throw;
        }
        catch (SmtpException ex)
        {
            // Don't retry on permanent failures
            return CommandResponse<bool>.Failure("Failed to send email: " + ex.Message);
        }
    }

    private bool IsTransient(SmtpException ex)
    {
        // Retry on network errors, timeouts, etc.
        return ex.StatusCode == SmtpStatusCode.ServiceNotAvailable ||
               ex.StatusCode == SmtpStatusCode.MailboxBusy ||
               ex.InnerException is TimeoutException;
    }
}
```

### Retry with Circuit Breaker Pattern

Combine retry with a circuit breaker for better resilience:

```csharp
public class CallExternalApiQuery : IQuery<ApiResponse>, IRetryConfiguration
{
    public string Endpoint { get; set; }

    public int MaxRetries => 3;
    public TimeSpan InitialDelay => TimeSpan.FromSeconds(1);
    public bool UseExponentialBackoff => true;
}

public class CallExternalApiQueryHandler : IQueryHandler<CallExternalApiQuery, ApiResponse>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICircuitBreakerPolicy _circuitBreaker;

    public CallExternalApiQueryHandler(
        IHttpClientFactory httpClientFactory,
        ICircuitBreakerPolicy circuitBreaker)
    {
        _httpClientFactory = httpClientFactory;
        _circuitBreaker = circuitBreaker;
    }

    public async Task<QueryResponse<ApiResponse>> HandleAsync(
        CallExternalApiQuery query,
        CancellationToken cancellationToken)
    {
        // Circuit breaker prevents retries if service is known to be down
        var response = await _circuitBreaker.ExecuteAsync(async () =>
        {
            var client = _httpClientFactory.CreateClient();
            return await client.GetAsync(query.Endpoint, cancellationToken);
        });

        var content = await response.Content.ReadAsStringAsync();
        return QueryResponse<ApiResponse>.Success(new ApiResponse { Data = content });
    }
}
```

## Best Practices

### 1. Use Retry for Transient Failures Only

Retry is appropriate for:
- Network timeouts
- Temporary service unavailability
- Database deadlocks
- Rate limiting (with appropriate backoff)

Do NOT retry for:
- Validation errors
- Authentication/authorization failures
- Business rule violations
- Permanent errors (404, 403, etc.)

### 2. Choose Appropriate Retry Counts

```csharp
// High-frequency, low-impact operations
public int MaxRetries => 5;
public TimeSpan InitialDelay => TimeSpan.FromMilliseconds(100);

// Critical operations (payments, data modifications)
public int MaxRetries => 2;
public TimeSpan InitialDelay => TimeSpan.FromSeconds(2);

// Idempotent read operations
public int MaxRetries => 3;
public TimeSpan InitialDelay => TimeSpan.FromSeconds(1);
```

### 3. Use Exponential Backoff

Always use exponential backoff to avoid overwhelming failing services:

```csharp
public bool UseExponentialBackoff => true;  // Recommended
```

### 4. Implement Idempotency

Ensure your operations are idempotent (safe to retry):

```csharp
public class CreateOrderCommand : ICommand<Order>, IRetryConfiguration
{
    public Guid IdempotencyKey { get; set; }  // Prevent duplicate orders
    public int CustomerId { get; set; }
    public List<OrderItem> Items { get; set; }

    public int MaxRetries => 3;
    public TimeSpan InitialDelay => TimeSpan.FromSeconds(1);
    public bool UseExponentialBackoff => true;
}

public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Order>
{
    public async Task<CommandResponse<Order>> HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        // Check if order already exists with this idempotency key
        var existing = await _orderRepository.GetByIdempotencyKeyAsync(
            command.IdempotencyKey,
            cancellationToken);

        if (existing != null)
        {
            return CommandResponse<Order>.Success(existing);
        }

        // Create new order
        var order = new Order
        {
            IdempotencyKey = command.IdempotencyKey,
            CustomerId = command.CustomerId,
            Items = command.Items
        };

        await _orderRepository.AddAsync(order, cancellationToken);
        return CommandResponse<Order>.Success(order);
    }
}
```

### 5. Monitor Retry Attempts

The retry decorator logs each retry attempt. Monitor these logs to identify:
- Services with high failure rates
- Operations that need retry tuning
- Potential system issues

```csharp
// Logs will show:
// "Retry attempt 1 of 3 for SendEmailCommand after 1000ms delay"
// "Retry attempt 2 of 3 for SendEmailCommand after 2000ms delay"
// "Retry attempt 3 of 3 for SendEmailCommand after 4000ms delay"
```

## Troubleshooting

### Retries Not Happening

1. Ensure the retry decorator is registered:
   ```csharp
   builder.AddCommandRetryDecorator();
   ```

2. Verify your command has the `[RetryCommand]` attribute:
   ```csharp
   [RetryCommand(3, 100, 200, 400)]
   public class MyCommand : ICommand<Result>
   ```

3. Check that retry count is > 0:
   ```csharp
   [RetryCommand(3)]  // Must be > 0
   ```

4. For queries, ensure `ApplyToAllQueries` is configured if you want retry without attribute:
   ```csharp
   builder.AddQueryRetryDecorator(applyToAllQueries: true);
   ```

### Too Many Retries

If you're seeing excessive retries:

1. Reduce retry count in the attribute:
   ```csharp
   [RetryCommand(2, 1000)]  // Reduced from 5 to 2
   ```

2. Increase delays between retries:
   ```csharp
   [RetryCommand(3, 5000)]  // 5 second delay between retries
   ```

3. Ensure you're not retrying permanent failures - catch and handle them in your handler

## Integration with Other Decorators

### With Exception Decorator

The Exception decorator should wrap the Retry decorator to catch final failures after all retries are exhausted:

```csharp
builder.AddCommandRetryDecorator()
       .AddCommandExceptionDecorator()  // Catches exceptions after retries exhausted
       .AddCommandHandlers();
```

See: [Exception Decorator Documentation](../Minded.Extensions.Exception/README.md)

### With Logging Decorator

The Logging decorator logs each retry attempt:

```csharp
builder.AddCommandRetryDecorator()
       .AddCommandLoggingDecorator()    // Logs retry attempts
       .AddCommandExceptionDecorator()
       .AddCommandHandlers();
```

See: [Logging Decorator Documentation](../Minded.Extensions.Logging/README.md)

### With Transaction Decorator

Be careful when combining Retry with Transaction - each retry will create a new transaction:

```csharp
// Transaction wraps retry - each retry gets a new transaction
builder.AddCommandRetryDecorator()
       .AddCommandTransactionDecorator()  // New transaction per retry
       .AddCommandHandlers();
```

See: [Transaction Decorator Documentation](../Minded.Extensions.Transaction/README.md)

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.Retry)
- [Main Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
