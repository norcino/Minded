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
using Minded.Extensions.Retry;

public class SendEmailCommand : ICommand<bool>, IRetryConfiguration
{
    public string To { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }

    // Retry configuration
    public int MaxRetries => 3;
    public TimeSpan InitialDelay => TimeSpan.FromSeconds(1);
    public bool UseExponentialBackoff => true;
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

### IRetryConfiguration Interface

Implement this interface on your command or query to configure retry behavior:

```csharp
public interface IRetryConfiguration
{
    /// <summary>
    /// Maximum number of retry attempts (default: 3)
    /// </summary>
    int MaxRetries { get; }

    /// <summary>
    /// Initial delay before first retry (default: 1 second)
    /// </summary>
    TimeSpan InitialDelay { get; }

    /// <summary>
    /// Whether to use exponential backoff (default: true)
    /// Delay doubles with each retry: 1s, 2s, 4s, 8s, etc.
    /// </summary>
    bool UseExponentialBackoff { get; }
}
```

### Example Configurations

#### Aggressive Retry (Fast, Many Attempts)

```csharp
public class CheckInventoryQuery : IQuery<InventoryStatus>, IRetryConfiguration
{
    public int ProductId { get; set; }

    public int MaxRetries => 5;
    public TimeSpan InitialDelay => TimeSpan.FromMilliseconds(100);
    public bool UseExponentialBackoff => true;
}

// Retry schedule: 100ms, 200ms, 400ms, 800ms, 1600ms
```

#### Conservative Retry (Slow, Few Attempts)

```csharp
public class ProcessPaymentCommand : ICommand<PaymentResult>, IRetryConfiguration
{
    public decimal Amount { get; set; }
    public string CardToken { get; set; }

    public int MaxRetries => 2;
    public TimeSpan InitialDelay => TimeSpan.FromSeconds(5);
    public bool UseExponentialBackoff => false;
}

// Retry schedule: 5s, 5s (fixed delay)
```

#### No Retry

```csharp
public class DeleteUserCommand : ICommand<bool>, IRetryConfiguration
{
    public int UserId { get; set; }

    public int MaxRetries => 0;  // No retries
    public TimeSpan InitialDelay => TimeSpan.Zero;
    public bool UseExponentialBackoff => false;
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

2. Verify your command implements `IRetryConfiguration`:
   ```csharp
   public class MyCommand : ICommand<Result>, IRetryConfiguration
   ```

3. Check that `MaxRetries > 0`:
   ```csharp
   public int MaxRetries => 3;  // Must be > 0
   ```

### Too Many Retries

If you're seeing excessive retries:

1. Reduce `MaxRetries`:
   ```csharp
   public int MaxRetries => 2;  // Reduced from 5
   ```

2. Increase `InitialDelay`:
   ```csharp
   public TimeSpan InitialDelay => TimeSpan.FromSeconds(5);  // Increased from 1s
   ```

3. Ensure you're not retrying permanent failures - catch and handle them in your handler

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.Retry)
- [Main Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
