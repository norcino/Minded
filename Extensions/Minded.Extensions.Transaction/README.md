# Minded.Extensions.Transaction

Transaction decorator for automatic database transaction management with support for nested transactions, configurable isolation levels, and automatic rollback on failure.

## Features

- **Automatic Transaction Scope** - Wraps command execution in a transaction
- **Nested Transaction Support** - Handles nested transactions correctly
- **Configurable Isolation Levels** - Set isolation level per command
- **Automatic Rollback** - Rolls back on exceptions or validation failures
- **Timeout Configuration** - Set transaction timeout per command
- **Async Support** - Full async/await support with proper transaction flow

## Installation

```bash
dotnet add package Minded.Extensions.Transaction
```

## Quick Start

### 1. Create a Command with Transaction Configuration

```csharp
using Minded.Framework.CQRS.Abstractions;
using Minded.Extensions.Transaction.Decorator;
using System.Transactions;

// Use default settings from TransactionOptions
[TransactionalCommand]
public class CreateOrderCommand : ICommand<Order>
{
    public int CustomerId { get; set; }
    public List<OrderItem> Items { get; set; }
}

// Or with custom configuration via attribute properties
[TransactionalCommand(
    IsolationLevel = IsolationLevel.Serializable,
    TimeoutSeconds = 30)]
public class CreatePaymentCommand : ICommand<Payment>
{
    public decimal Amount { get; set; }
}
```

### 2. Configure Transaction Decorator

```csharp
using Minded.Extensions.Configuration;

services.AddMinded(builder =>
{
    // Add transaction decorator for commands
    builder.AddCommandTransactionDecorator();

    // Queries typically don't need transactions
    // but you can add it if needed
    builder.AddQueryTransactionDecorator();
});
```

### 3. Automatic Transaction Management

```csharp
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Order>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryService _inventoryService;

    public async Task<CommandResponse<Order>> HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        // All database operations are wrapped in a transaction
        var order = new Order
        {
            CustomerId = command.CustomerId,
            Items = command.Items
        };

        await _orderRepository.AddAsync(order, cancellationToken);

        // If this fails, the entire transaction rolls back
        await _inventoryService.ReserveItemsAsync(command.Items, cancellationToken);

        // Transaction commits automatically if no exceptions
        return CommandResponse<Order>.Success(order);
    }
}
```

## Configuration Options

### Decorator Registration Options

The transaction decorator can be registered with or without configuration:

```csharp
// Default registration (uses appsettings.json configuration)
builder.AddCommandTransactionDecorator();
builder.AddQueryTransactionDecorator();

// With programmatic configuration
builder.AddCommandTransactionDecorator(options =>
{
    options.DefaultIsolationLevel = IsolationLevel.ReadCommitted;
    options.DefaultTimeout = TimeSpan.FromMinutes(2);
    options.RollbackOnUnsuccessfulResponse = true;
    options.EnableLogging = true;
});
```

### TransactionOptions Class

Configure default transaction behavior for all commands/queries:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DefaultTransactionScopeOption` | `TransactionScopeOption` | `Required` | How the transaction scope participates in ambient transactions |
| `DefaultTransactionScopeOptionProvider` | `Func<TransactionScopeOption>` | `null` | Dynamic provider for transaction scope option (takes precedence over static value) |
| `DefaultIsolationLevel` | `IsolationLevel` | `ReadCommitted` | Default isolation level when not specified in attribute or interface |
| `DefaultIsolationLevelProvider` | `Func<IsolationLevel>` | `null` | Dynamic provider for isolation level (takes precedence over static value) |
| `DefaultTimeout` | `TimeSpan` | `1 minute` | Default transaction timeout. Transactions exceeding this will be rolled back |
| `DefaultTimeoutProvider` | `Func<TimeSpan>` | `null` | Dynamic provider for transaction timeout (takes precedence over static value) |
| `RollbackOnUnsuccessfulResponse` | `bool` | `true` | If `true`, rolls back when `ICommandResponse.Successful` is `false`. If `false`, only exceptions cause rollback |
| `RollbackOnUnsuccessfulResponseProvider` | `Func<bool>` | `null` | Dynamic provider for rollback behavior (takes precedence over static value) |
| `EnableLogging` | `bool` | `true` | If `true`, logs transaction start/complete/rollback events at Information level |
| `EnableLoggingProvider` | `Func<bool>` | `null` | Dynamic provider for logging enablement (takes precedence over static value) |

### Configuration via appsettings.json

```json
{
  "Minded": {
    "TransactionOptions": {
      "DefaultIsolationLevel": "ReadCommitted",
      "DefaultTimeout": "00:02:00",
      "RollbackOnUnsuccessfulResponse": true,
      "EnableLogging": true
    }
  }
}
```

### Runtime Configuration with Providers

All `TransactionOptions` properties support dynamic runtime configuration via provider functions. This allows you to change transaction behavior at runtime based on feature flags, configuration services, or other runtime conditions.

**Example: Runtime Configuration with Feature Flags**

```csharp
builder.AddCommandTransactionDecorator(options =>
{
    // Static configuration (fallback values)
    options.DefaultIsolationLevel = IsolationLevel.ReadCommitted;
    options.DefaultTimeout = TimeSpan.FromMinutes(1);

    // Dynamic configuration via providers (takes precedence)
    options.DefaultIsolationLevelProvider = () =>
        _configService.GetValue("transaction-isolation-level", IsolationLevel.ReadCommitted);

    options.DefaultTimeoutProvider = () =>
        TimeSpan.FromSeconds(_configService.GetValue("transaction-timeout-seconds", 60));

    options.RollbackOnUnsuccessfulResponseProvider = () =>
        _featureFlags.IsEnabled("transaction-rollback-on-unsuccessful");

    options.EnableLoggingProvider = () =>
        _featureFlags.IsEnabled("transaction-logging");
});
```

**Example: Environment-Based Configuration**

```csharp
builder.AddCommandTransactionDecorator(options =>
{
    // Stricter isolation in production
    options.DefaultIsolationLevelProvider = () =>
        _environment.IsProduction() ? IsolationLevel.Serializable : IsolationLevel.ReadCommitted;

    // Longer timeout in production
    options.DefaultTimeoutProvider = () =>
        _environment.IsProduction() ? TimeSpan.FromMinutes(2) : TimeSpan.FromSeconds(30);
});
```

**How Providers Work:**

- Providers are invoked **each time** a transaction is initiated
- If a provider is set, it takes precedence over the static property value
- If a provider returns `null` or is not set, the static property value is used
- This enables true runtime configuration without application restart

**GetEffective Methods:**

The `TransactionOptions` class provides `GetEffective*()` methods that handle the provider logic:

```csharp
public IsolationLevel GetEffectiveDefaultIsolationLevel()
{
    return DefaultIsolationLevelProvider?.Invoke() ?? DefaultIsolationLevel;
}

public TimeSpan GetEffectiveDefaultTimeout()
{
    return DefaultTimeoutProvider?.Invoke() ?? DefaultTimeout;
}

public bool GetEffectiveRollbackOnUnsuccessfulResponse()
{
    return RollbackOnUnsuccessfulResponseProvider?.Invoke() ?? RollbackOnUnsuccessfulResponse;
}

public bool GetEffectiveEnableLogging()
{
    return EnableLoggingProvider?.Invoke() ?? EnableLogging;
}
```

### Transaction Attributes

Configure transaction behavior per command/query using attributes:

**[TransactionalCommand] Attribute:**

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TransactionCommandAttribute : Attribute
{
    /// <summary>
    /// Transaction scope option (default: Required)
    /// </summary>
    public TransactionScopeOption TransactionScopeOption { get; set; }

    /// <summary>
    /// Transaction isolation level (default: ReadCommitted)
    /// </summary>
    public IsolationLevel IsolationLevel { get; set; }

    /// <summary>
    /// Transaction timeout in seconds (0 = use default from TransactionOptions)
    /// </summary>
    public int TimeoutSeconds { get; set; }
}
```

**[TransactionalQuery] Attribute:**

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TransactionQueryAttribute : Attribute
{
    // Same properties as TransactionCommandAttribute
    public TransactionScopeOption TransactionScopeOption { get; set; }
    public IsolationLevel IsolationLevel { get; set; }
    public int TimeoutSeconds { get; set; }
}
```

**Attribute Usage Examples:**

```csharp
// Use defaults from TransactionOptions
[TransactionalCommand]
public class CreateUserCommand : ICommand<User> { }

// Custom isolation level
[TransactionalCommand(IsolationLevel = IsolationLevel.Serializable)]
public class ProcessPaymentCommand : ICommand<Payment> { }

// Custom timeout (30 seconds)
[TransactionalCommand(TimeoutSeconds = 30)]
public class QuickOperationCommand : ICommand<Result> { }

// Full configuration
[TransactionalCommand(
    IsolationLevel = IsolationLevel.Serializable,
    TimeoutSeconds = 60,
    TransactionScopeOption = TransactionScopeOption.Required)]
public class CriticalOperationCommand : ICommand<Result> { }
```

**Note:** Attribute properties override TransactionOptions defaults.

### Isolation Levels

```csharp
using System.Transactions;
using Minded.Extensions.Transaction.Decorator;

// ReadUncommitted - Lowest isolation, highest performance, dirty reads possible
[TransactionalCommand(IsolationLevel = IsolationLevel.ReadUncommitted)]
public class ReportCommand : ICommand<Report> { }

// ReadCommitted - Default, prevents dirty reads
[TransactionalCommand(IsolationLevel = IsolationLevel.ReadCommitted)]
public class UpdateUserCommand : ICommand<User> { }

// RepeatableRead - Prevents dirty and non-repeatable reads
[TransactionalCommand(IsolationLevel = IsolationLevel.RepeatableRead)]
public class InventoryCommand : ICommand<Result> { }

// Serializable - Highest isolation, lowest performance, prevents all anomalies
[TransactionalCommand(IsolationLevel = IsolationLevel.Serializable)]
public class FinancialCommand : ICommand<Payment> { }

// Snapshot - SQL Server specific, uses row versioning
[TransactionalCommand(IsolationLevel = IsolationLevel.Snapshot)]
public class AuditCommand : ICommand<AuditLog> { }
```

### Timeout Configuration

```csharp
using Minded.Extensions.Transaction.Decorator;

// Short timeout (10 seconds)
[TransactionalCommand(TimeoutSeconds = 10)]
public class QuickOperationCommand : ICommand<Result> { }

// Longer timeout (5 minutes = 300 seconds)
[TransactionalCommand(TimeoutSeconds = 300)]
public class LongRunningCommand : ICommand<Result> { }

// Use default timeout from TransactionOptions (TimeoutSeconds = 0)
[TransactionalCommand]
public class StandardCommand : ICommand<Result> { }
```

## Advanced Usage

### Nested Transactions

The transaction decorator handles nested transactions using `TransactionScopeOption.Required`:

```csharp
using Minded.Extensions.Transaction.Decorator;

[TransactionalCommand(
    IsolationLevel = IsolationLevel.ReadCommitted,
    TimeoutSeconds = 60)]
public class CreateOrderWithPaymentCommand : ICommand<Order>
{
    public Order Order { get; set; }
    public Payment Payment { get; set; }
}

public class CreateOrderWithPaymentCommandHandler : ICommandHandler<CreateOrderWithPaymentCommand, Order>
{
    private readonly IMediator _mediator;

    public async Task<CommandResponse<Order>> HandleAsync(
        CreateOrderWithPaymentCommand command,
        CancellationToken cancellationToken)
    {
        // Outer transaction starts here

        // This command also has [TransactionalCommand] attribute
        // It will join the outer transaction (nested)
        var orderResult = await _mediator.ProcessCommandAsync(
            new CreateOrderCommand { Order = command.Order },
            cancellationToken);

        if (!orderResult.Success)
            return CommandResponse<Order>.Failure("Failed to create order");

        // This also joins the same transaction
        var paymentResult = await _mediator.ProcessCommandAsync(
            new ProcessPaymentCommand { Payment = command.Payment },
            cancellationToken);

        if (!paymentResult.Success)
        {
            // Payment failed - entire transaction rolls back
            // Order creation is also rolled back
            return CommandResponse<Order>.Failure("Payment failed");
        }

        // Both operations succeed - transaction commits
        return CommandResponse<Order>.Success(orderResult.Result);
    }
}
```

### Distributed Transactions

For distributed transactions across multiple databases or services:

```csharp
using Minded.Extensions.Transaction.Decorator;

[TransactionalCommand(
    IsolationLevel = IsolationLevel.Serializable,  // Use Serializable for financial transactions
    TimeoutSeconds = 30)]
public class TransferFundsCommand : ICommand<bool>
{
    public int FromAccountId { get; set; }
    public int ToAccountId { get; set; }
    public decimal Amount { get; set; }
}

public class TransferFundsCommandHandler : ICommandHandler<TransferFundsCommand, bool>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAuditService _auditService;

    public async Task<CommandResponse<bool>> HandleAsync(
        TransferFundsCommand command,
        CancellationToken cancellationToken)
    {
        // Debit from source account
        await _accountRepository.DebitAsync(
            command.FromAccountId,
            command.Amount,
            cancellationToken);

        // Credit to destination account
        await _accountRepository.CreditAsync(
            command.ToAccountId,
            command.Amount,
            cancellationToken);

        // Log audit trail (also in transaction)
        await _auditService.LogTransferAsync(
            command.FromAccountId,
            command.ToAccountId,
            command.Amount,
            cancellationToken);

        // All operations commit together or roll back together
        return CommandResponse<bool>.Success(true);
    }
}
```

## Best Practices

### 1. Use Transactions for Commands, Not Queries

```csharp
using Minded.Extensions.Transaction.Decorator;

// Good - transaction for state-changing command
[TransactionalCommand]
public class CreateUserCommand : ICommand<User> { }

// Avoid - transaction for read-only query (unnecessary overhead)
[TransactionalQuery]  // Generally not recommended
public class GetUserQuery : IQuery<User> { }
```

### 2. Choose Appropriate Isolation Levels

```csharp
using Minded.Extensions.Transaction.Decorator;

// Financial transactions - use Serializable
[TransactionalCommand(IsolationLevel = IsolationLevel.Serializable)]
public class ProcessPaymentCommand : ICommand<Payment> { }

// Regular CRUD - use ReadCommitted (default)
[TransactionalCommand(IsolationLevel = IsolationLevel.ReadCommitted)]
public class UpdateUserCommand : ICommand<User> { }

// Reporting/analytics - use ReadUncommitted (if dirty reads acceptable)
[TransactionalCommand(IsolationLevel = IsolationLevel.ReadUncommitted)]
public class GenerateReportCommand : ICommand<Report> { }
```

### 3. Set Realistic Timeouts

```csharp
using Minded.Extensions.Transaction.Decorator;

// Quick operations - short timeout (5 seconds)
[TransactionalCommand(TimeoutSeconds = 5)]
public class UpdateStatusCommand : ICommand<bool> { }

// Batch operations - longer timeout (10 minutes = 600 seconds)
[TransactionalCommand(TimeoutSeconds = 600)]
public class ImportDataCommand : ICommand<int> { }
```

### 4. Handle Transaction Failures Gracefully

```csharp
public async Task<CommandResponse<Order>> HandleAsync(
    CreateOrderCommand command,
    CancellationToken cancellationToken)
{
    try
    {
        // Transaction operations
        var order = await _orderRepository.CreateAsync(command.Order);
        return CommandResponse<Order>.Success(order);
    }
    catch (TransactionAbortedException ex)
    {
        // Transaction was aborted (timeout, deadlock, etc.)
        return CommandResponse<Order>.Failure("Transaction failed: " + ex.Message);
    }
    catch (DbUpdateConcurrencyException ex)
    {
        // Concurrency conflict
        return CommandResponse<Order>.Failure("Order was modified by another user");
    }
}
```

### 5. Avoid Long-Running Transactions

```csharp
// Avoid - long-running transaction locks resources
public async Task<CommandResponse<Report>> HandleAsync(...)
{
    // Don't do this in a transaction
    var data = await _externalApi.FetchDataAsync();  // Slow external call
    await _repository.SaveAsync(data);
    return CommandResponse<Report>.Success(report);
}

// Good - minimize transaction scope
public async Task<CommandResponse<Report>> HandleAsync(...)
{
    // Fetch data outside transaction
    var data = await _externalApi.FetchDataAsync();

    // Only database operations in transaction
    await _repository.SaveAsync(data);
    return CommandResponse<Report>.Success(report);
}
```

## Performance Considerations

### Transaction Overhead

- **ReadUncommitted**: Lowest overhead, no locks
- **ReadCommitted**: Moderate overhead, read locks released immediately
- **RepeatableRead**: Higher overhead, read locks held until commit
- **Serializable**: Highest overhead, range locks

### Deadlock Prevention

```csharp
// Always access resources in the same order to prevent deadlocks
public async Task HandleAsync(TransferFundsCommand command, CancellationToken ct)
{
    // Order account IDs to prevent deadlocks
    var firstAccountId = Math.Min(command.FromAccountId, command.ToAccountId);
    var secondAccountId = Math.Max(command.FromAccountId, command.ToAccountId);

    var firstAccount = await _repository.GetByIdAsync(firstAccountId, ct);
    var secondAccount = await _repository.GetByIdAsync(secondAccountId, ct);

    // Process transfer...
}
```

## Troubleshooting

### Transaction Timeout

If you're seeing transaction timeout errors:

1. Increase the timeout in the attribute:
   ```csharp
   [TransactionalCommand(TimeoutSeconds = 300)]  // 5 minutes
   public class LongRunningCommand : ICommand<Result> { }
   ```

2. Or increase the default timeout in TransactionOptions:
   ```csharp
   builder.AddCommandTransactionDecorator(options =>
   {
       options.DefaultTimeout = TimeSpan.FromMinutes(5);
   });
   ```

3. Optimize your handler to reduce execution time

4. Consider breaking the operation into smaller transactions

### Deadlocks

If you're experiencing deadlocks:

1. Access resources in a consistent order
2. Use lower isolation levels if appropriate
3. Keep transactions short
4. Add retry logic for deadlock exceptions

### Distributed Transaction Errors

If distributed transactions fail:

1. Ensure MSDTC is enabled (Windows)
2. Check firewall settings
3. Consider using Saga pattern instead of distributed transactions

## Integration with Other Decorators

### With Validation Decorator

Validation can run inside or outside the transaction depending on your needs:

```csharp
// Validation INSIDE transaction (if validation reads from database)
builder.AddCommandValidationDecorator()
       .AddCommandTransactionDecorator()  // Wraps validation
       .AddCommandHandlers();

// Validation OUTSIDE transaction (fail fast, no DB reads)
builder.AddCommandTransactionDecorator()
       .AddCommandValidationDecorator()   // Validates before transaction
       .AddCommandHandlers();
```

See: [Validation Decorator Documentation](../Minded.Extensions.Validation/README.md)

### With Exception Decorator

The Exception decorator should wrap the Transaction decorator to catch transaction errors:

```csharp
builder.AddCommandTransactionDecorator()
       .AddCommandExceptionDecorator()  // Catches transaction exceptions
       .AddCommandHandlers();
```

See: [Exception Decorator Documentation](../Minded.Extensions.Exception/README.md)

### With Retry Decorator

Be careful when combining Retry with Transaction - each retry creates a new transaction:

```csharp
// Each retry attempt gets a new transaction
builder.AddCommandRetryDecorator()
       .AddCommandTransactionDecorator()  // New transaction per retry
       .AddCommandHandlers();
```

See: [Retry Decorator Documentation](../Minded.Extensions.Retry/README.md)

### With Logging Decorator

The Logging decorator logs transaction lifecycle events:

```csharp
builder.AddCommandTransactionDecorator(options =>
{
    options.EnableLogging = true;  // Logs transaction start/commit/rollback
})
.AddCommandLoggingDecorator()
.AddCommandHandlers();
```

See: [Logging Decorator Documentation](../Minded.Extensions.Logging/README.md)

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.Transaction)
- [Main Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)

