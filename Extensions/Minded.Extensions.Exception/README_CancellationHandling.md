# CancellationToken and OperationCanceledException Handling

## Overview

The Minded framework properly handles `CancellationToken` throughout the entire CQRS pipeline and provides special handling for `OperationCanceledException` to distinguish between real errors and cancelled requests.

## Why Special Handling for OperationCanceledException?

### The Problem

Without special handling, when an `OperationCanceledException` is thrown:

1. **ASP.NET Core returns HTTP 500** (Internal Server Error)
2. **Logs it as an error** (pollutes error logs)
3. **Triggers monitoring alerts** (false positives)
4. **Doesn't distinguish** between server errors and client disconnects

### The Solution

The Minded framework handles `OperationCanceledException` specially:

1. **Logs as Information** (not an error) in the Exception decorators
2. **Returns HTTP 499** (Client Closed Request) from RestMediator
3. **Prevents false error alerts** in monitoring systems
4. **Provides clear distinction** between errors and cancellations

## HTTP Status Codes

### Standard Behavior (Without Minded)

```
Client disconnects → OperationCanceledException → HTTP 500 Internal Server Error ❌
```

### Minded Framework Behavior

```
Client disconnects → OperationCanceledException → HTTP 499 Client Closed Request ✅
```

### Status Code Reference

- **499 Client Closed Request** - Client disconnected before response (nginx convention, widely adopted)
- **408 Request Timeout** - Server-side timeout (alternative, less specific)
- **500 Internal Server Error** - Actual server error (NOT for cancellations)

## How It Works

### 1. Exception Decorators

The `ExceptionQueryHandlerDecorator` and `ExceptionCommandHandlerDecorator` catch `OperationCanceledException` separately:

```csharp
public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
{
    try
    {
        return await DecoratedQueryHandler.HandleAsync(query, cancellationToken);
    }
    catch (OperationCanceledException)
    {
        // Log as information, not error
        _logger.LogInformation("Query {QueryType} was cancelled", typeof(TQuery).Name);
        throw; // Re-throw to let RestMediator handle it
    }
    catch (System.Exception ex)
    {
        // Real errors are logged as errors
        _logger.LogError(ex, ex.Message);
        throw new QueryHandlerException<TQuery, TResult>(query, "QueryHandlerException: ...", ex);
    }
}
```

**Key Points:**
- Catches `OperationCanceledException` **before** the general exception handler
- Logs at **Information** level (not Error)
- Re-throws the exception to let RestMediator handle the HTTP response

### 2. RestMediator

The `RestMediator` catches `OperationCanceledException` and returns HTTP 499:

```csharp
public async Task<IActionResult> ProcessRestQueryAsync<TResult>(
    RestOperation operation, 
    IQuery<TResult> query, 
    CancellationToken cancellationToken = default)
{
    try
    {
        var result = await ProcessQueryAsync(query, cancellationToken);
        return _rulesProcessor.ProcessQueryRules(operation, result);
    }
    catch (OperationCanceledException)
    {
        // Return 499 Client Closed Request
        return new StatusCodeResult(499);
    }
}
```

**Key Points:**
- Catches `OperationCanceledException` at the REST boundary
- Returns **HTTP 499** instead of letting it propagate to ASP.NET Core
- Same handling for queries and commands

## Common Cancellation Scenarios

### 1. Client Disconnects

**Scenario:** User closes browser tab while request is processing

```csharp
[HttpGet]
public async Task<IActionResult> GetTransactions(CancellationToken cancellationToken)
{
    // ASP.NET Core automatically provides cancellationToken
    // If user closes browser, token is cancelled
    return await _mediator.ProcessRestQueryAsync(
        RestOperation.GetMany, 
        new GetTransactionsQuery(), 
        cancellationToken);
}
```

**Result:**
- Database query is cancelled (if using EF Core with cancellationToken)
- Logs: `Information: Query GetTransactionsQuery was cancelled`
- HTTP Response: **499 Client Closed Request**

### 2. Request Timeout

**Scenario:** Operation takes too long and times out

```csharp
[HttpPost]
public async Task<IActionResult> ImportLargeFile(IFormFile file, CancellationToken cancellationToken)
{
    // Create a 5-minute timeout
    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
        cancellationToken, 
        timeoutCts.Token);
    
    return await _mediator.ProcessRestCommandAsync<int>(
        RestOperation.CreateWithContent, 
        new ImportFileCommand { File = file }, 
        linkedCts.Token);
}
```

**Result:**
- After 5 minutes, `OperationCanceledException` is thrown
- Logs: `Information: Command ImportFileCommand was cancelled`
- HTTP Response: **499 Client Closed Request**

### 3. Manual Cancellation

**Scenario:** Application logic decides to cancel an operation

```csharp
public async Task<List<Transaction>> HandleAsync(
    GetTransactionsQuery query, 
    CancellationToken cancellationToken = default)
{
    var transactions = new List<Transaction>();
    
    foreach (var userId in query.UserIds)
    {
        // Check if cancellation was requested
        cancellationToken.ThrowIfCancellationRequested();
        
        var userTransactions = await _context.Transactions
            .Where(t => t.UserId == userId)
            .ToListAsync(cancellationToken);
            
        transactions.AddRange(userTransactions);
    }
    
    return transactions;
}
```

**Result:**
- If cancelled during processing, `OperationCanceledException` is thrown
- Logs: `Information: Query GetTransactionsQuery was cancelled`
- HTTP Response: **499 Client Closed Request**

## Logging Behavior

### Cancellation (Information Level)

```
[2024-11-08 10:30:45] [Information] Query GetTransactionsQuery was cancelled
```

- **Level:** Information
- **Message:** Simple, clean message
- **No stack trace**
- **Won't trigger error alerts**

### Real Error (Error Level)

```
[2024-11-08 10:30:45] [Error] QueryHandlerException: {"UserId":123}
System.InvalidOperationException: Database connection failed
   at Service.Transaction.QueryHandler.GetTransactionsQueryHandler.HandleAsync(...)
   ...
```

- **Level:** Error
- **Message:** Detailed with query JSON
- **Full stack trace**
- **Will trigger error alerts**

## Best Practices

### 1. Always Accept CancellationToken

```csharp
// ✅ Good
public async Task<List<Transaction>> HandleAsync(
    GetTransactionsQuery query, 
    CancellationToken cancellationToken = default)
{
    return await _context.Transactions.ToListAsync(cancellationToken);
}

// ❌ Bad
public async Task<List<Transaction>> HandleAsync(GetTransactionsQuery query)
{
    return await _context.Transactions.ToListAsync(); // Can't be cancelled!
}
```

### 2. Pass Token to All Async Operations

```csharp
// ✅ Good
public async Task<Transaction> HandleAsync(
    CreateTransactionCommand command, 
    CancellationToken cancellationToken = default)
{
    await _context.Transactions.AddAsync(command.Transaction, cancellationToken);
    await _context.SaveChangesAsync(cancellationToken);
    return command.Transaction;
}

// ❌ Bad
public async Task<Transaction> HandleAsync(
    CreateTransactionCommand command, 
    CancellationToken cancellationToken = default)
{
    await _context.Transactions.AddAsync(command.Transaction); // Missing token
    await _context.SaveChangesAsync(); // Missing token
    return command.Transaction;
}
```

### 3. Check Cancellation in Long Loops

```csharp
// ✅ Good
public async Task ProcessBatchAsync(
    List<Transaction> transactions, 
    CancellationToken cancellationToken = default)
{
    foreach (var transaction in transactions)
    {
        cancellationToken.ThrowIfCancellationRequested(); // Check before each iteration
        await ProcessTransactionAsync(transaction, cancellationToken);
    }
}

// ❌ Bad
public async Task ProcessBatchAsync(
    List<Transaction> transactions, 
    CancellationToken cancellationToken = default)
{
    foreach (var transaction in transactions)
    {
        await ProcessTransactionAsync(transaction, cancellationToken);
        // If cancelled, might process many more before checking
    }
}
```

### 4. Don't Catch OperationCanceledException in Handlers

```csharp
// ✅ Good - Let it propagate
public async Task<List<Transaction>> HandleAsync(
    GetTransactionsQuery query, 
    CancellationToken cancellationToken = default)
{
    return await _context.Transactions.ToListAsync(cancellationToken);
    // OperationCanceledException will propagate to decorators
}

// ❌ Bad - Don't swallow it
public async Task<List<Transaction>> HandleAsync(
    GetTransactionsQuery query, 
    CancellationToken cancellationToken = default)
{
    try
    {
        return await _context.Transactions.ToListAsync(cancellationToken);
    }
    catch (OperationCanceledException)
    {
        return new List<Transaction>(); // Wrong! This hides the cancellation
    }
}
```

## Monitoring and Observability

### What to Monitor

1. **HTTP 499 Rate** - High rate might indicate:
   - Slow queries/commands
   - Client-side timeout issues
   - Network problems

2. **HTTP 500 Rate** - Should NOT include cancellations
   - Only real server errors
   - More accurate error rate

3. **Cancellation Logs** - Information level
   - Track which operations are cancelled most
   - Identify performance bottlenecks

### Example Monitoring Query (Application Insights)

```kusto
// Real errors (exclude cancellations)
requests
| where resultCode == 500
| where customDimensions.Exception !contains "OperationCanceledException"

// Cancellations
requests
| where resultCode == 499

// Cancellation rate by operation
requests
| where resultCode == 499
| summarize count() by operation_Name
| order by count_ desc
```

## Summary

The Minded framework provides **production-ready cancellation handling**:

✅ **Proper HTTP status codes** - 499 for cancellations, 500 for real errors  
✅ **Clean logging** - Information for cancellations, Error for real errors  
✅ **No false alerts** - Monitoring systems won't trigger on cancellations  
✅ **Resource efficiency** - Cancelled operations stop immediately  
✅ **Best practices** - Follows .NET async/await patterns  

This makes your APIs more robust, observable, and efficient! 🎯

