# Minded.Extensions.WebApi

ASP.NET Core Web API integration with RestMediator for automatic HTTP status code mapping, response formatting, and seamless CQRS pattern integration.

## Features

- **RestMediator** - HTTP-aware mediator that returns `IActionResult`
- **Automatic HTTP Status Code Mapping** - Maps command/query results to appropriate HTTP codes
- **Validation Error Formatting** - Returns 400 Bad Request with validation details
- **Cancellation Token Support** - Returns HTTP 499 for cancelled requests
- **Security Exception Mapping** - Maps authentication/authorization exceptions to 401/403
- **Success Response Formatting** - Returns 200 OK with result payload
- **RESTful Conventions** - Follows REST best practices

## Installation

```bash
dotnet add package Minded.Extensions.WebApi
dotnet add package Minded.Extensions.Configuration
```

## Quick Start

### 1. Configure Services

```csharp
using Minded.Extensions.Configuration;
using Minded.Extensions.WebApi;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        // Register Minded with decorators
        // First registered = innermost (runs last); last registered = outermost (runs first)
        services.AddMinded(Configuration, assembly => assembly.FullName.StartsWith("YourApp"), builder =>
        {
            builder.AddMediator();

            builder.AddCommandValidationDecorator();
            builder.AddCommandLoggingDecorator();
            builder.AddCommandExceptionDecorator();   // outermost: must be registered last
            builder.AddCommandHandlers();

            builder.AddQueryValidationDecorator();
            builder.AddQueryLoggingDecorator();
            builder.AddQueryExceptionDecorator();     // outermost: must be registered last
            builder.AddQueryHandlers();

            // Register RestMediator
            builder.AddRestMediator();
        });
    }
}
```

### 2. Create a Controller

```csharp
using Microsoft.AspNetCore.Mvc;
using Minded.Extensions.WebApi;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IRestMediator _mediator;

    public UsersController(IRestMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id, CancellationToken cancellationToken)
    {
        var query = new GetUserByIdQuery { UserId = id };
        return await _mediator.ProcessRestQueryAsync(RestOperation.GetSingle, query, cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        return await _mediator.ProcessRestCommandAsync(RestOperation.CreateWithContent, command, cancellationToken);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(
        int id,
        [FromBody] UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        command.UserId = id;
        return await _mediator.ProcessRestCommandAsync(RestOperation.UpdateWithContent, command, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        var command = new DeleteUserCommand { UserId = id };
        return await _mediator.ProcessRestCommandAsync(RestOperation.Delete, command, cancellationToken);
    }
}
```

### 3. HTTP Status Code Mapping

RestMediator automatically maps results to HTTP status codes based on the `RestOperation` and the outcome:

```csharp
// Success - Returns 200 OK
IActionResult result = await _mediator.ProcessRestQueryAsync(
    RestOperation.GetSingle, new GetUserByIdQuery { UserId = 123 }, cancellationToken);
// HTTP 200 OK
// Body: { "id": 123, "name": "John Doe", "email": "john@example.com" }

// Not Found - Returns 404 Not Found (GetSingle with a null result)
IActionResult result = await _mediator.ProcessRestQueryAsync(
    RestOperation.GetSingle, new GetUserByIdQuery { UserId = 999 }, cancellationToken);
// HTTP 404 Not Found

// Created - Returns 201 Created with the new resource in the body
IActionResult result = await _mediator.ProcessRestCommandAsync(
    RestOperation.CreateWithContent, new CreateUserCommand { Email = "john@example.com" }, cancellationToken);
// HTTP 201 Created

// Validation Failure - Returns 400 Bad Request with the full response (Successful, OutcomeEntries)
IActionResult result = await _mediator.ProcessRestCommandAsync(
    RestOperation.CreateWithContent, new CreateUserCommand { Email = "invalid" }, cancellationToken);
// HTTP 400 Bad Request

// Cancelled - Returns 499 Client Closed Request
// The client cancels/disconnects while the command or query is running
// HTTP 499 Client Closed Request
```

## HTTP Status Code Reference (Default Rules)

The mappings below are produced by the built-in `DefaultRestRulesProvider`. Rules are evaluated in order and the first matching rule wins.

### Query rules

| Operation | Condition | HTTP Status Code | Response Body |
|-----------|-----------|-----------------|---------------|
| `GetSingle` | Result is not null and successful | 200 OK | Result |
| `GetSingle` | Result is null | 404 Not Found | Empty |
| `GetMany` | Successful (or plain result) | 200 OK | Result |
| `AnyGet` | Unsuccessful `IQueryResponse` (e.g. validation failure) | 400 Bad Request | Full response |
| `Any` | Outcome entry with `GenericErrorCodes.NotAuthorized` | 401 Unauthorized | Full response |
| `Any` | Outcome entry with `GenericErrorCodes.NotAuthenticated` | 403 Forbidden | Full response |

### Command rules

| Operation | Condition | HTTP Status Code | Response Body |
|-----------|-----------|-----------------|---------------|
| `Create` | Successful | 201 Created | Empty |
| `CreateWithContent` | Successful | 201 Created | Result |
| `Create` / `CreateWithContent` | Unsuccessful | 400 Bad Request | Full response |
| `Update` | Successful | 204 No Content | Empty |
| `UpdateWithContent` | Successful | 200 OK | Result |
| `Update` / `UpdateWithContent` | Outcome entry with `GenericErrorCodes.SubjectNotFound` | 404 Not Found | Empty / Full response |
| `Update` / `UpdateWithContent` | Other unsuccessful outcome | 400 Bad Request | Empty / Full response |
| `Patch` | Successful | 204 No Content | Empty |
| `PatchWithContent` | Successful | 200 OK | Result |
| `Patch` / `PatchWithContent` | Outcome entry with `GenericErrorCodes.SubjectNotFound` | 404 Not Found | Empty / Full response |
| `Patch` / `PatchWithContent` | Other unsuccessful outcome | 400 Bad Request | Empty / Full response |
| `Delete` | Successful | 200 OK | Empty |
| `Delete` | Outcome entry with `GenericErrorCodes.SubjectNotFound` | 404 Not Found | Empty |
| `Action` | Successful | 200 OK | Empty |
| `ActionWithContent` | Successful | 200 OK | Full response |
| `ActionWithResultContent` | Successful | 200 OK | Result |
| `Action` / `ActionWithContent` | Unsuccessful | 400 Bad Request | Full response |
| `Any` | Outcome entry with `GenericErrorCodes.NotAuthorized` | 401 Unauthorized | Full response |
| `Any` | Outcome entry with `GenericErrorCodes.NotAuthenticated` | 403 Forbidden | Full response |

### Exceptions caught by RestMediator

| Exception thrown by the pipeline | HTTP Status Code |
|----------------------------------|-----------------|
| `UnauthorizedAccessException`, `InvalidCredentialException`, `AuthenticationException` | 401 Unauthorized |
| `SecurityException` | 403 Forbidden |
| `OperationCanceledException` | 499 Client Closed Request |

Any other exception is not caught by `RestMediator` and propagates to the ASP.NET Core pipeline (by default resulting in 500 Internal Server Error).

When no rule matches, the processor falls back to 200 OK for null or successful results and 400 Bad Request for unsuccessful responses.

## RestOperation Enum

The `RestOperation` enum identifies the logical REST operation being performed, which determines the HTTP status code mapping. It is a `[Flags]` enum, so multiple values can be combined with bitwise OR when defining a rule that should apply to several operations.

| Value | Description |
|-------|-------------|
| `Any` | Wildcard matching any operation; use in catch-all rules |
| `Action` | Fire-and-forget action command (POST) - 200 OK with no body |
| `ActionWithContent` | Action command returning the full response object in the body - 200 OK |
| `ActionWithResultContent` | Action command returning a typed result in the body - 200 OK |
| `AnyAction` | Composite: matches `Action`, `ActionWithContent` and `ActionWithResultContent` |
| `Create` | Create command (POST) - 201 Created with no body |
| `CreateWithContent` | Create command returning the new resource in the body - 201 Created |
| `AnyCreate` | Composite: matches `Create` and `CreateWithContent` |
| `Delete` | Delete command (DELETE) - 200 OK with no body, 404 when the subject is not found |
| `GetMany` | Query returning a collection of resources - 200 OK (even if empty) |
| `GetSingle` | Query returning a single resource by key - 200 OK or 404 Not Found |
| `AnyGet` | Composite: matches `GetMany` and `GetSingle` |
| `Patch` | Partial-update command (PATCH) without a response body - 204 No Content |
| `PatchWithContent` | Partial-update command (PATCH) returning the patched resource - 200 OK |
| `AnyPatch` | Composite: matches `Patch` and `PatchWithContent` |
| `Update` | Full-update command (PUT) without a response body - 204 No Content |
| `UpdateWithContent` | Full-update command (PUT) returning the updated resource - 200 OK |
| `AnyUpdate` | Composite: matches `Update` and `UpdateWithContent` |

### Usage with RestOperation

```csharp
[HttpGet]
public async Task<IActionResult> GetAll()
{
    return await _mediator.ProcessRestQueryAsync(
        RestOperation.GetMany,
        new GetUsersQuery());
}

[HttpGet("{id}")]
public async Task<IActionResult> GetById(int id)
{
    return await _mediator.ProcessRestQueryAsync(
        RestOperation.GetSingle,
        new GetUserByIdQuery(id));
}

[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateUserCommand command)
{
    return await _mediator.ProcessRestCommandAsync(
        RestOperation.CreateWithContent,
        command);
}

[HttpPut("{id}")]
public async Task<IActionResult> Update(int id, [FromBody] UpdateUserCommand command)
{
    command.Id = id;
    return await _mediator.ProcessRestCommandAsync(
        RestOperation.UpdateWithContent,
        command);
}

[HttpDelete("{id}")]
public async Task<IActionResult> Delete(int id)
{
    return await _mediator.ProcessRestCommandAsync(
        RestOperation.Delete,
        new DeleteUserCommand(id));
}
```

### Relationship with IMediator

`RestMediator` derives from `Mediator`, so an `IRestMediator` also exposes the inherited `IMediator` methods `ProcessQueryAsync<TResult>(IQuery<TResult>, CancellationToken)`, `ProcessCommandAsync(ICommand, CancellationToken)` and `ProcessCommandAsync<TResult>(ICommand<TResult>, CancellationToken)`. These return the raw query result or `ICommandResponse` without any HTTP status code mapping. In controllers always use the `ProcessRestQueryAsync` / `ProcessRestCommandAsync` methods, which run the same pipeline and then translate the outcome into an `IActionResult`.

## Configuration Options

### Decorator Registration

The WebApi extension has **no runtime configuration options**. Behavior is customized through:

1. **IRestRulesProvider** - Custom HTTP response mapping rules
2. **RestOperation Enum** - Passed as parameter to `ProcessRestCommandAsync`/`ProcessRestQueryAsync` methods

```csharp
// Default registration (uses DefaultRestRulesProvider and DefaultRulesProcessor)
services.AddMinded(Configuration, assembly => assembly.FullName.StartsWith("YourApp"), builder =>
{
    builder.AddRestMediator();
});

// With custom REST rules provider (and optionally a custom rules processor)
services.AddMinded(Configuration, assembly => assembly.FullName.StartsWith("YourApp"), builder =>
{
    builder.AddRestMediator(ServiceLifetime.Transient, typeof(CustomRestRulesProvider));
});
```

**Note:** Unlike other decorators (Logging, Exception, Transaction), the WebApi extension does not accept configuration options during registration. Customization is done by passing custom `IRestRulesProvider` and/or `IRulesProcessor` implementation types to `AddRestMediator`.

### Custom REST Rules

You can customize how RestMediator maps operations to HTTP responses by implementing `IRestRulesProvider`:

```csharp
using System.Net;
using Minded.Extensions.WebApi;
using Minded.Framework.CQRS.Command;

public class CustomRestRulesProvider : IRestRulesProvider
{
    public IEnumerable<ICommandRestRule> GetCommandRules() => new ICommandRestRule[]
    {
        // Successful create - 201 Created with the result in the body
        new CommandRestRule(RestOperation.Create, HttpStatusCode.Created,
            ContentResponse.Result, response => response.Successful),

        // Successful update - 204 No Content
        new CommandRestRule(RestOperation.Update, HttpStatusCode.NoContent,
            ContentResponse.None, response => response.Successful)
    };

    public IEnumerable<IQueryRestRule> GetQueryRules() => new IQueryRestRule[]
    {
        // GetSingle with a result - 200 OK with the result in the body
        new QueryRestRule(RestOperation.GetSingle, HttpStatusCode.OK,
            ContentResponse.Result, result => result != null),

        // GetSingle without a result - 404 Not Found
        new QueryRestRule(RestOperation.GetSingle, HttpStatusCode.NotFound,
            ContentResponse.None, result => result == null)
    };
}
```

Rules are evaluated in the order they are returned; the first rule whose operation matches and whose condition predicate returns `true` wins. A `null` condition matches every response for that operation.

Register your custom rules provider by passing its type to `AddRestMediator`:

```csharp
builder.AddRestMediator(ServiceLifetime.Transient, typeof(CustomRestRulesProvider));
```

### ContentResponse Options

The `ContentResponse` enum controls what gets returned in the HTTP response body:

```csharp
public enum ContentResponse
{
    None,    // No content in response body (e.g. 204 No Content)
    Result,  // Only the Result property of the typed response
    Full     // Full response including metadata (Successful, OutcomeEntries, etc.)
}
```

For commands and queries that do not carry a typed result, `ContentResponse.Result` falls back to returning the full response object.

## Advanced Usage

### Integration with OData

RestMediator works seamlessly with OData queries:

```csharp
using Microsoft.AspNetCore.OData.Query;

[HttpGet]
public async Task<IActionResult> Get([FromQuery] ODataQueryOptions<User> queryOptions)
{
    var query = new GetUsersQuery();
    query.ApplyODataQueryOptions(queryOptions);

    return await _mediator.ProcessRestQueryAsync(
        RestOperation.GetMany,
        query);
}
```

### Signalling Not Found

For `RestOperation.GetSingle` queries, returning `null` from the handler produces a 404 Not Found:

```csharp
// In your query handler
public async Task<User> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken)
{
    // Returning null maps to 404 Not Found for RestOperation.GetSingle
    return await _context.Users
        .FirstOrDefaultAsync(u => u.Id == query.UserId, cancellationToken);
}
```

For commands (`Update`, `UpdateWithContent`, `Patch`, `PatchWithContent`, `Delete`), return an unsuccessful `ICommandResponse` containing an `OutcomeEntry` with `GenericErrorCodes.SubjectNotFound` (from `Minded.Extensions.Exception`) to produce a 404 Not Found:

```csharp
return new CommandResponse(new OutcomeEntry(
    nameof(command.UserId), "User with ID {0} not found",
    command.UserId, Severity.Error, GenericErrorCodes.SubjectNotFound));
```

### Non-CRUD Operations

For operations that don't fit standard REST patterns, use the `Action` operations (optionally combined with custom rules):

```csharp
[HttpPost("archive/{id}")]
public async Task<IActionResult> Archive(int id, CancellationToken cancellationToken)
{
    return await _mediator.ProcessRestCommandAsync(
        RestOperation.Action,
        new ArchiveUserCommand(id),
        cancellationToken);
}
```

## Integration with Other Decorators

### With Validation Decorator

```csharp
services.AddMinded(Configuration, a => a.FullName.StartsWith("YourApp"), builder =>
{
    builder.AddCommandValidationDecorator();  // Validates before execution
    builder.AddCommandExceptionDecorator();   // Outermost: catches exceptions
    builder.AddCommandHandlers();

    builder.AddRestMediator();  // Maps validation errors to 400 Bad Request
});
```

### With Logging Decorator

```csharp
services.AddMinded(Configuration, a => a.FullName.StartsWith("YourApp"), builder =>
{
    builder.AddCommandLoggingDecorator();     // Logs all operations
    builder.AddCommandExceptionDecorator();   // Outermost: catches and logs exceptions
    builder.AddCommandHandlers();

    builder.AddRestMediator();  // Returns appropriate HTTP codes
});
```

### With Caching Decorator

```csharp
services.AddMinded(Configuration, a => a.FullName.StartsWith("YourApp"), builder =>
{
    builder.AddQueryMemoryCacheDecorator();   // Caches query results
    builder.AddQueryLoggingDecorator();       // Logs cache hits/misses
    builder.AddQueryHandlers();

    builder.AddRestMediator();  // Returns cached results with 200 OK
});
```

## Best Practices

### 1. Use CancellationToken

Always pass the cancellation token to support request cancellation:

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(int id, CancellationToken cancellationToken)
{
    var query = new GetUserByIdQuery { UserId = id };
    return await _mediator.ProcessRestQueryAsync(
        RestOperation.GetSingle,
        query,
        cancellationToken);
}
```

### 2. Use Appropriate RestOperation

Choose the correct `RestOperation` for your endpoint:

- **GetSingle** - Retrieving a single resource (returns 404 if not found)
- **GetMany** - Retrieving a collection (returns 200 even if empty)
- **CreateWithContent** - Creating a resource and returning it (201 Created)
- **UpdateWithContent** - Updating and returning the updated resource (200 OK)
- **Delete** - Deleting a resource (200 OK with no body)

### 3. Validate Input with Model Binding

```csharp
[HttpPost]
public async Task<IActionResult> CreateUser(
    [FromBody] CreateUserCommand command,
    CancellationToken cancellationToken)
{
    // Model binding validates the command structure
    // The validation decorator ([ValidateCommand]) validates business rules
    return await _mediator.ProcessRestCommandAsync(
        RestOperation.CreateWithContent,
        command,
        cancellationToken);
}
```

### 4. Follow RESTful Conventions

```csharp
// GET for queries
[HttpGet]
public async Task<IActionResult> GetUsers()
    => await _mediator.ProcessRestQueryAsync(RestOperation.GetMany, new GetUsersQuery());

// POST for creating resources
[HttpPost]
public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
    => await _mediator.ProcessRestCommandAsync(RestOperation.CreateWithContent, command);

// PUT for full updates
[HttpPut("{id}")]
public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserCommand command)
    => await _mediator.ProcessRestCommandAsync(RestOperation.UpdateWithContent, command);

// DELETE for deletions
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteUser(int id)
    => await _mediator.ProcessRestCommandAsync(RestOperation.Delete, new DeleteUserCommand(id));
```

### 5. Use Proper HTTP Status Codes

- **200 OK** - Successful GET, `UpdateWithContent`, `PatchWithContent`, `Delete`, or `Action*` operations
- **201 Created** - Successful `Create` / `CreateWithContent`
- **204 No Content** - Successful `Update` / `Patch` (no response body)
- **400 Bad Request** - Validation errors and other business failures
- **404 Not Found** - Resource not found
- **500 Internal Server Error** - Unhandled exceptions (returned by the ASP.NET Core pipeline)

## Troubleshooting

### Issue: Always Getting 500 Internal Server Error

**Cause**: Exception decorator not registered or exceptions not being caught.

**Solution**: Ensure exception decorator is registered (last, so it is outermost):

```csharp
services.AddMinded(Configuration, a => a.FullName.StartsWith("YourApp"), builder =>
{
    builder.AddCommandExceptionDecorator();
    builder.AddQueryExceptionDecorator();
    builder.AddCommandHandlers();
    builder.AddQueryHandlers();
});
```

### Issue: Validation Errors Not Returning 400 Bad Request

**Cause**: Validation decorator not registered or not applied to command/query.

**Solution**:
1. Register validation decorator
2. Add `[ValidateCommand]` or `[ValidateQuery]` attribute
3. Implement validator

```csharp
services.AddMinded(Configuration, a => a.FullName.StartsWith("YourApp"), builder =>
{
    builder.AddCommandValidationDecorator();
    builder.AddCommandHandlers();
});

[ValidateCommand]
public class CreateUserCommand : ICommand<User> { }
```

### Issue: Getting 404 for Existing Resources

**Cause**: Using `RestOperation.GetSingle` when the handler returns null (a null result maps to 404 Not Found).

**Solution**: Ensure your handler actually loads and returns the resource:

```csharp
public async Task<User> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken)
{
    // Verify the lookup criteria match the data; a null result produces 404
    return await _context.Users
        .FirstOrDefaultAsync(u => u.Id == query.UserId, cancellationToken);
}
```

### Issue: CancellationToken Not Working

**Cause**: Not passing cancellation token through the chain.

**Solution**: Pass cancellation token to all async operations:

```csharp
public async Task<IActionResult> GetUser(int id, CancellationToken cancellationToken)
{
    return await _mediator.ProcessRestQueryAsync(
        RestOperation.GetSingle,
        new GetUserByIdQuery(id),
        cancellationToken);  // ← Pass it here
}

// In handler
public async Task<User> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken)
{
    return await _context.Users
        .FirstOrDefaultAsync(u => u.Id == query.UserId, cancellationToken);  // ← And here
}
```

## Performance Considerations

- **RestMediator is lightweight** - Minimal overhead compared to direct mediator usage
- **Use caching for expensive queries** - Combine with caching decorator for better performance
- **Avoid N+1 queries** - Use eager loading or projection in query handlers
- **Consider response size** - Use `ContentResponse.None` for operations that don't need to return data

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.WebApi)
- [Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
