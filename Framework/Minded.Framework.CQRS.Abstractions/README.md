# Minded.Framework.CQRS.Abstractions

Core abstractions for CQRS pattern implementation including ICommand, IQuery, ICommandHandler, IQueryHandler interfaces, response types, query trait interfaces, and sanitization pipeline interfaces.

## Features

- **Command Interfaces** - ICommand, ICommandHandler, ICommandResponse
- **Query Interfaces** - IQuery, IQueryHandler, IQueryResponse
- **Message Abstractions** - IMessage, IMessageResponse
- **Response Types** - Success/failure tracking with outcome entries
- **Outcome Tracking** - Validation and business rule results
- **Query Traits** - Opt-in pagination, ordering, filtering, counting and expansion (ICanTop, ICanSkip, ICanOrderBy, ICanCount, ICanFilterExpression, ICanExpand)
- **Sanitization Pipeline** - Interfaces for logging sanitization (ILoggingSanitizer, ILoggingSanitizerPipeline)
- **Severity Levels** - Outcome entry severity classification

## Installation

```bash
dotnet add package Minded.Framework.CQRS.Abstractions
```

## Core Interfaces

### Messages

#### IMessage

Shared base interface for every command and query. Each message carries a `TraceId` used to correlate all commands and queries originating from the same request across logs and distributed traces:

```csharp
public interface IMessage
{
    Guid TraceId { get; }
}
```

#### IMessageResponse

Base response interface shared by `ICommandResponse` and `IQueryResponse<TResult>`. Carries the outcome of a command or query execution:

```csharp
public interface IMessageResponse
{
    bool Successful { get; set; }
    List<IOutcomeEntry> OutcomeEntries { get; set; }
}
```

A `Successful` value of `false` does not necessarily mean an exception occurred; business-rule failures are also expressed as unsuccessful responses via `OutcomeEntries`.

### Commands

#### ICommand

Marker interface for commands that don't return a result:

```csharp
public interface ICommand : IMessage
{
}
```

#### ICommand&lt;TResult&gt;

Interface for commands that return a result. It extends `ICommand` (and therefore `IMessage`):

```csharp
public interface ICommand<TResult> : ICommand
{
}
```

#### ICommandHandler&lt;TCommand&gt;

Handler for commands without a result:

```csharp
public interface ICommandHandler<TCommand> where TCommand : ICommand
{
    Task<ICommandResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
```

#### ICommandHandler&lt;TCommand, TResult&gt;

Handler for commands with a result:

```csharp
public interface ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<ICommandResponse<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
```

#### ICommandResponse

Response for commands without a result. The interface itself declares no members; it inherits `Successful` and `OutcomeEntries` from `IMessageResponse`:

```csharp
public interface ICommandResponse : IMessageResponse
{
}
```

#### ICommandResponse&lt;TResult&gt;

Response for commands with a result. Adds a read-only, covariant `Result`:

```csharp
public interface ICommandResponse<out TResult> : ICommandResponse
{
    TResult Result { get; }
}
```

### Queries

#### IQuery&lt;TResult&gt;

Interface for queries that return a result:

```csharp
public interface IQuery<TResult> : IMessage
{
}
```

#### IQueryHandler&lt;TQuery, TResult&gt;

Handler for queries:

```csharp
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
```

#### IQueryResponse&lt;TResult&gt;

Structured query response wrapping the result. Inherits `Successful` and `OutcomeEntries` from `IMessageResponse`:

```csharp
public interface IQueryResponse<out TResult> : IMessageResponse
{
    TResult Result { get; }
}
```

### Query Traits

The `Minded.Framework.CQRS.Query.Trait` namespace contains opt-in trait interfaces. Implement them on query classes to declare which OData-style capabilities the query supports (pagination, ordering, filtering, counting, expansion):

#### ICanCount

```csharp
public interface ICanCount
{
    bool CountOnly { get; set; }   // when true the query only returns the count and no data
    bool Count { get; set; }       // when true the query also returns the number of matching rows
    int CountValue { get; set; }   // populated with the count of records matching the query criteria
}
```

`CountValue` reflects the number of records matching the query criteria (before pagination with `Top`/`Skip` is applied). Setting `CountOnly` returns the count with no data.

#### ICanTop

```csharp
public interface ICanTop
{
    int? Top { get; set; }   // maximum number of results to return; null = no limit
}
```

#### ICanSkip

```csharp
public interface ICanSkip
{
    int? Skip { get; set; }  // number of results to skip; null = skip none
}
```

`ICanTop` and `ICanSkip` are commonly used together to implement server-side pagination.

#### ICanOrderBy

```csharp
public interface ICanOrderBy
{
    IList<OrderDescriptor> OrderBy { get; set; }
}
```

Descriptors are applied in list order (primary sort first, then secondary, etc.). `OrderDescriptor` is constructed with a direction and a property name:

```csharp
var descriptor = new OrderDescriptor(Order.Descending, "CreatedAt");
```

`Order` is an enum with values `Ascending` and `Descending`.

#### ICanFilterExpression&lt;T&gt;

```csharp
public interface ICanFilterExpression<T> : ICanFilter<T>
{
    Expression<Func<T, bool>> Filter { get; set; }
}
```

`ICanFilter<T>` is an empty marker interface intended only as a base for creating new filtering traits with different filter representations.

#### ICanExpand

```csharp
public interface ICanExpand
{
    string[] Expand { get; set; }   // navigation property names to expand
}
```

### Outcome Tracking

#### IOutcomeEntry

Represents a single detail output associated with a validation or a processing. Outcome entries are not necessarily failures; they can also carry warnings or informational details:

```csharp
public interface IOutcomeEntry
{
    object AttemptedValue { get; }          // read-only: the value that caused the alert or failure
    string ErrorCode { get; set; }
    string UniqueErrorCode { get; set; }    // unique identifier of an error, usable as a dictionary key of known errors
    string Message { get; }                 // read-only: the detail message
    string PropertyName { get; }            // read-only: the name of the property
    string ResourceName { get; set; }       // resource name used for building the message
    Severity Severity { get; set; }
    string ToString();                      // textual representation of the entry
}
```

`AttemptedValue`, `Message` and `PropertyName` are read-only on the interface and are set through the `OutcomeEntry` constructors (the concrete `OutcomeEntry` class ships in the `Minded.Framework.CQRS` package).

#### Severity Enum

```csharp
public enum Severity
{
    Error,
    Warning,
    Info
}
```

### Sanitization Pipeline

#### ILoggingSanitizer

Interface for custom sanitizers that process data before logging:

```csharp
public interface ILoggingSanitizer
{
    IDictionary<string, object> Sanitize(IDictionary<string, object> data, Type sourceType);
}
```

Sanitizers are applied in registration order; each receives the output of the previous one.

#### ILoggingSanitizerPipeline

Pipeline that orchestrates multiple sanitizers. It first converts the object (typically a command or query) to a dictionary, then applies all registered sanitizers:

```csharp
public interface ILoggingSanitizerPipeline
{
    IDictionary<string, object> Sanitize(object obj);
    void RegisterSanitizer(ILoggingSanitizer sanitizer);
    void ExcludeProperties(Type interfaceType, params string[] memberNames);
}
```

`ExcludeProperties` scopes exclusions to a specific interface — for example `ExcludeProperties(typeof(ILoggable), "LoggingTemplate")` excludes that property from any object implementing `ILoggable`. Registration methods should only be called during application startup. The default pipeline implementation ships in the `Minded.Framework.CQRS` package.

## Usage Examples

### Creating a Command

Every command implements `IMessage` through `ICommand`, so it must expose a `TraceId`:

```csharp
using System;
using Minded.Framework.CQRS.Command;

// Command without result
public class DeleteUserCommand : ICommand
{
    public DeleteUserCommand(int userId, Guid? traceId = null)
    {
        UserId = userId;
        TraceId = traceId ?? Guid.NewGuid();
    }

    public int UserId { get; }
    public Guid TraceId { get; }
}

// Command with result
public class CreateUserCommand : ICommand<User>
{
    public CreateUserCommand(string name, string email, Guid? traceId = null)
    {
        Name = name;
        Email = email;
        TraceId = traceId ?? Guid.NewGuid();
    }

    public string Name { get; }
    public string Email { get; }
    public Guid TraceId { get; }
}
```

### Creating a Command Handler

The concrete response types (`CommandResponse`, `CommandResponse<TResult>`, `OutcomeEntry`) ship in the `Minded.Framework.CQRS` package:

```csharp
using Minded.Framework.CQRS.Command;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, User>
{
    private readonly IDbContext _context;

    public CreateUserCommandHandler(IDbContext context)
    {
        _context = context;
    }

    public async Task<ICommandResponse<User>> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            Name = command.Name,
            Email = command.Email
        };

        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // The result constructor sets Successful = true automatically
        return new CommandResponse<User>(user);
    }
}
```

### Creating a Query

```csharp
using System;
using Minded.Framework.CQRS.Query;

public class GetUserByIdQuery : IQuery<User>
{
    public GetUserByIdQuery(int userId, Guid? traceId = null)
    {
        UserId = userId;
        TraceId = traceId ?? Guid.NewGuid();
    }

    public int UserId { get; }
    public Guid TraceId { get; }
}
```

### Creating a Query Handler

```csharp
using Minded.Framework.CQRS.Query;

public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, User>
{
    private readonly IDbContext _context;

    public GetUserByIdQueryHandler(IDbContext context)
    {
        _context = context;
    }

    public async Task<User> HandleAsync(
        GetUserByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == query.UserId, cancellationToken);
    }
}
```

### Using Outcome Entries

`OutcomeEntry` instances are created through constructors (`PropertyName`, `Message` and `AttemptedValue` cannot be set via object initializers):

```csharp
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;

public async Task<ICommandResponse<User>> HandleAsync(
    CreateUserCommand command,
    CancellationToken cancellationToken = default)
{
    // Check business rules
    var existingUser = await _context.Users
        .FirstOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

    if (existingUser != null)
    {
        return CommandResponse<User>.Error(
            new OutcomeEntry(
                nameof(command.Email),
                "Email already exists",
                command.Email,
                Severity.Error,
                "DUPLICATE_EMAIL"));
    }

    // Create user
    var user = new User { Name = command.Name, Email = command.Email };
    await _context.Users.AddAsync(user, cancellationToken);
    await _context.SaveChangesAsync(cancellationToken);

    return CommandResponse<User>.Success(user);
}
```

## Best Practices

1. **Commands Change State** - Use commands for Create, Update, Delete operations
2. **Queries Read Data** - Use queries for Get, List, Search operations
3. **Handlers Are Focused** - Each handler should do one thing
4. **Carry a TraceId** - Default it to `Guid.NewGuid()` so every command/query can be correlated across logs and traces
5. **Use CancellationToken** - Always support cancellation for long-running operations
6. **Return Outcome Entries** - Use outcome entries for validation and business rule violations
7. **Async All The Way** - Use async/await throughout the chain

## See Also

- [Minded.Framework.CQRS](https://www.nuget.org/packages/Minded.Framework.CQRS) - Concrete implementations
- [Minded.Framework.Mediator](https://www.nuget.org/packages/Minded.Framework.Mediator) - Mediator pattern implementation
- [Main Documentation](https://github.com/norcino/Minded#readme) - Complete framework documentation

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Framework.CQRS.Abstractions)
- [Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
