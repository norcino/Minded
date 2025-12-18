# Minded.Framework.CQRS.Abstractions

Core abstractions for CQRS pattern implementation including ICommand, IQuery, ICommandHandler, IQueryHandler interfaces, response types, and sanitization pipeline interfaces.

## Features

- **Command Interfaces** - ICommand, ICommandHandler, ICommandResponse
- **Query Interfaces** - IQuery, IQueryHandler
- **Message Abstractions** - IMessage, IMessageResponse
- **Response Types** - Success/failure tracking with outcome entries
- **Outcome Tracking** - Validation and business rule results
- **Sanitization Pipeline** - Interfaces for logging sanitization (ILoggingSanitizer, ILoggingSanitizerPipeline)
- **Severity Levels** - Outcome entry severity classification

## Installation

```bash
dotnet add package Minded.Framework.CQRS.Abstractions
```

## Core Interfaces

### Commands

#### ICommand

Marker interface for commands that don't return a result:

```csharp
public interface ICommand : IMessage
{
}
```

#### ICommand<TResult>

Interface for commands that return a result:

```csharp
public interface ICommand<TResult> : IMessage
{
}
```

#### ICommandHandler<TCommand>

Handler for commands without a result:

```csharp
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task<ICommandResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
```

#### ICommandHandler<TCommand, TResult>

Handler for commands with a result:

```csharp
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<ICommandResponse<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
```

#### ICommandResponse

Response for commands without a result:

```csharp
public interface ICommandResponse : IMessageResponse
{
    bool Successful { get; set; }
    IList<IOutcomeEntry> OutcomeEntries { get; set; }
}
```

#### ICommandResponse<TResult>

Response for commands with a result:

```csharp
public interface ICommandResponse<TResult> : ICommandResponse
{
    TResult Result { get; set; }
}
```

### Queries

#### IQuery<TResult>

Interface for queries that return a result:

```csharp
public interface IQuery<TResult> : IMessage
{
}
```

#### IQueryHandler<TQuery, TResult>

Handler for queries:

```csharp
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
```

### Outcome Tracking

#### IOutcomeEntry

Represents a validation or business rule outcome:

```csharp
public interface IOutcomeEntry
{
    string PropertyName { get; set; }
    string Message { get; set; }
    Severity Severity { get; set; }
}
```

#### Severity Enum

```csharp
public enum Severity
{
    Information = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
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

#### ILoggingSanitizerPipeline

Pipeline that orchestrates multiple sanitizers:

```csharp
public interface ILoggingSanitizerPipeline
{
    IDictionary<string, object> Sanitize(object source);
    void RegisterSanitizer(ILoggingSanitizer sanitizer);
    void RegisterExcludedProperty(string propertyPath);
}
```

## Usage Examples

### Creating a Command

```csharp
using Minded.Framework.CQRS.Abstractions.Command;

// Command without result
public class DeleteUserCommand : ICommand
{
    public int UserId { get; set; }
}

// Command with result
public class CreateUserCommand : ICommand<User>
{
    public string Name { get; set; }
    public string Email { get; set; }
}
```

### Creating a Command Handler

```csharp
using Minded.Framework.CQRS.Abstractions.Command;

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

        return new CommandResponse<User>(user) { Successful = true };
    }
}
```

### Creating a Query

```csharp
using Minded.Framework.CQRS.Abstractions.Query;

public class GetUserByIdQuery : IQuery<User>
{
    public int UserId { get; set; }
}
```

### Creating a Query Handler

```csharp
using Minded.Framework.CQRS.Abstractions.Query;

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

```csharp
public async Task<ICommandResponse<User>> HandleAsync(
    CreateUserCommand command,
    CancellationToken cancellationToken = default)
{
    var response = new CommandResponse<User>();

    // Check business rules
    var existingUser = await _context.Users
        .FirstOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

    if (existingUser != null)
    {
        response.Successful = false;
        response.OutcomeEntries.Add(new OutcomeEntry
        {
            PropertyName = nameof(command.Email),
            Message = "Email already exists",
            Severity = Severity.Error
        });
        return response;
    }

    // Create user
    var user = new User { Name = command.Name, Email = command.Email };
    await _context.Users.AddAsync(user, cancellationToken);
    await _context.SaveChangesAsync(cancellationToken);

    response.Result = user;
    response.Successful = true;
    return response;
}
```

## Best Practices

1. **Commands Change State** - Use commands for Create, Update, Delete operations
2. **Queries Read Data** - Use queries for Get, List, Search operations
3. **Handlers Are Focused** - Each handler should do one thing
4. **Use CancellationToken** - Always support cancellation for long-running operations
5. **Return Outcome Entries** - Use outcome entries for validation and business rule violations
6. **Async All The Way** - Use async/await throughout the chain

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
