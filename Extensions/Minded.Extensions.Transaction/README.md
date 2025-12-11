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
using Minded.Extensions.Transaction;
using System.Transactions;

public class CreateOrderCommand : ICommand<Order>, ITransactionConfiguration
{
    public int CustomerId { get; set; }
    public List<OrderItem> Items { get; set; }

    // Transaction configuration
    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
    public TimeSpan Timeout => TimeSpan.FromSeconds(30);
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

### ITransactionConfiguration Interface

Implement this interface on your command to configure transaction behavior:

```csharp
public interface ITransactionConfiguration
{
    /// <summary>
    /// Transaction isolation level (default: ReadCommitted)
    /// </summary>
    IsolationLevel IsolationLevel { get; }

    /// <summary>
    /// Transaction timeout (default: 1 minute)
    /// </summary>
    TimeSpan Timeout { get; }
}
```

### Isolation Levels

```csharp
using System.Transactions;

public class MyCommand : ICommand<Result>, ITransactionConfiguration
{
    // ReadUncommitted - Lowest isolation, highest performance, dirty reads possible
    public IsolationLevel IsolationLevel => IsolationLevel.ReadUncommitted;

    // ReadCommitted - Default, prevents dirty reads
    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

    // RepeatableRead - Prevents dirty and non-repeatable reads
    public IsolationLevel IsolationLevel => IsolationLevel.RepeatableRead;

    // Serializable - Highest isolation, lowest performance, prevents all anomalies
    public IsolationLevel IsolationLevel => IsolationLevel.Serializable;

    // Snapshot - SQL Server specific, uses row versioning
    public IsolationLevel IsolationLevel => IsolationLevel.Snapshot;
}
```

### Timeout Configuration

```csharp
public class QuickOperationCommand : ICommand<Result>, ITransactionConfiguration
{
    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
    public TimeSpan Timeout => TimeSpan.FromSeconds(10);  // Short timeout
}

public class LongRunningCommand : ICommand<Result>, ITransactionConfiguration
{
    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
    public TimeSpan Timeout => TimeSpan.FromMinutes(5);  // Longer timeout
}
```

## Advanced Usage

### Nested Transactions

The transaction decorator handles nested transactions using `TransactionScopeOption.Required`:

```csharp
public class CreateOrderWithPaymentCommand : ICommand<Order>, ITransactionConfiguration
{
    public Order Order { get; set; }
    public Payment Payment { get; set; }

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
    public TimeSpan Timeout => TimeSpan.FromSeconds(60);
}

public class CreateOrderWithPaymentCommandHandler : ICommandHandler<CreateOrderWithPaymentCommand, Order>
{
    private readonly IMediator _mediator;

    public async Task<CommandResponse<Order>> HandleAsync(
        CreateOrderWithPaymentCommand command,
        CancellationToken cancellationToken)
    {
        // Outer transaction starts here

        // This command also has ITransactionConfiguration
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
public class TransferFundsCommand : ICommand<bool>, ITransactionConfiguration
{
    public int FromAccountId { get; set; }
    public int ToAccountId { get; set; }
    public decimal Amount { get; set; }

    // Use Serializable for financial transactions
    public IsolationLevel IsolationLevel => IsolationLevel.Serializable;
    public TimeSpan Timeout => TimeSpan.FromSeconds(30);
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
// Good - transaction for state-changing command
public class CreateUserCommand : ICommand<User>, ITransactionConfiguration { }

// Avoid - transaction for read-only query (unnecessary overhead)
public class GetUserQuery : IQuery<User>, ITransactionConfiguration { }
```

### 2. Choose Appropriate Isolation Levels

```csharp
// Financial transactions - use Serializable
public class ProcessPaymentCommand : ICommand<Payment>, ITransactionConfiguration
{
    public IsolationLevel IsolationLevel => IsolationLevel.Serializable;
}

// Regular CRUD - use ReadCommitted (default)
public class UpdateUserCommand : ICommand<User>, ITransactionConfiguration
{
    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}

// Reporting/analytics - use ReadUncommitted (if dirty reads acceptable)
public class GenerateReportCommand : ICommand<Report>, ITransactionConfiguration
{
    public IsolationLevel IsolationLevel => IsolationLevel.ReadUncommitted;
}
```

### 3. Set Realistic Timeouts

```csharp
// Quick operations - short timeout
public class UpdateStatusCommand : ICommand<bool>, ITransactionConfiguration
{
    public TimeSpan Timeout => TimeSpan.FromSeconds(5);
}

// Batch operations - longer timeout
public class ImportDataCommand : ICommand<int>, ITransactionConfiguration
{
    public TimeSpan Timeout => TimeSpan.FromMinutes(10);
}
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

1. Increase the timeout:
   ```csharp
   public TimeSpan Timeout => TimeSpan.FromMinutes(5);
   ```

2. Optimize your handler to reduce execution time

3. Consider breaking the operation into smaller transactions

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

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.Transaction)
- [Main Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
