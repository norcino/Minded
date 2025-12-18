# Minded.Extensions.WebApi

ASP.NET Core Web API integration with RestMediator for automatic HTTP status code mapping, response formatting, and seamless CQRS pattern integration.

## Features

- **RestMediator** - HTTP-aware mediator that returns `IActionResult`
- **Automatic HTTP Status Code Mapping** - Maps command/query results to appropriate HTTP codes
- **Validation Error Formatting** - Returns 400 Bad Request with validation details
- **Cancellation Token Support** - Returns HTTP 499 for cancelled requests
- **Exception Handling** - Returns 500 Internal Server Error with sanitized messages
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
        services.AddMinded(builder =>
        {
            builder.AddCommandValidationDecorator();
            builder.AddCommandLoggingDecorator();
            builder.AddCommandExceptionDecorator();

            builder.AddQueryValidationDecorator();
            builder.AddQueryLoggingDecorator();
            builder.AddQueryExceptionDecorator();
        });

        // Register RestMediator
        services.AddRestMediator();
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
        return await _mediator.ProcessQueryAsync(query, cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        return await _mediator.ProcessCommandAsync(command, cancellationToken);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(
        int id,
        [FromBody] UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        command.UserId = id;
        return await _mediator.ProcessCommandAsync(command, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        var command = new DeleteUserCommand { UserId = id };
        return await _mediator.ProcessCommandAsync(command, cancellationToken);
    }
}
```

### 3. HTTP Status Code Mapping

RestMediator automatically maps results to HTTP status codes:

```csharp
// Success - Returns 200 OK
var result = await _mediator.ProcessQueryAsync(new GetUserQuery { Id = 123 });
// HTTP 200 OK
// Body: { "id": 123, "name": "John Doe", "email": "john@example.com" }

// Validation Failure - Returns 400 Bad Request
var result = await _mediator.ProcessCommandAsync(new CreateUserCommand { Email = "invalid" });
// HTTP 400 Bad Request
// Body: { "errors": ["Email must be a valid email address"] }

// Not Found - Returns 404 Not Found
var result = await _mediator.ProcessQueryAsync(new GetUserQuery { Id = 999 });
// HTTP 404 Not Found
// Body: { "message": "User not found" }

// Exception - Returns 500 Internal Server Error
var result = await _mediator.ProcessQueryAsync(new GetUserQuery { Id = 123 });
// HTTP 500 Internal Server Error
// Body: { "message": "An error occurred processing your request" }

// Cancelled - Returns 499 Client Closed Request
// User cancels the request
// HTTP 499 Client Closed Request
```

## HTTP Status Code Reference

| Scenario | HTTP Status Code | Response Body |
|----------|-----------------|---------------|
| Success | 200 OK | Result object |
| Validation Error | 400 Bad Request | Validation errors |
| Not Found | 404 Not Found | Error message |
| Exception | 500 Internal Server Error | Sanitized error message |
| Cancelled | 499 Client Closed Request | Cancellation message |

## RestOperation Enum

The `RestOperation` enum defines the type of REST operation being performed, which determines the HTTP status code mapping:

```csharp
public enum RestOperation
{
    GetSingle,           // GET single resource - 200 OK or 404 Not Found
    GetMany,             // GET collection - 200 OK (even if empty)
    Create,              // POST - 201 Created with no content
    CreateWithContent,   // POST - 201 Created with result in body
    Update,              // PUT - 204 No Content
    UpdateWithContent,   // PUT - 200 OK with result in body
    Delete,              // DELETE - 204 No Content
    DeleteWithContent,   // DELETE - 200 OK with result in body
    Custom               // Custom operation - use custom rules
}
```

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

## Configuration Options

### Decorator Registration

The WebApi extension has **no runtime configuration options**. Behavior is customized through:

1. **IRestRulesProvider** - Custom HTTP response mapping rules
2. **RestOperation Enum** - Passed as parameter to `ProcessRestCommandAsync`/`ProcessRestQueryAsync` methods

```csharp
// Default registration (uses default REST rules)
services.AddMinded(builder =>
{
    builder.AddRestMediator();
});

// With custom REST rules provider
services.AddMinded(builder =>
{
    builder.AddRestMediator<CustomRestRulesProvider>();
});
```

**Note:** Unlike other decorators (Logging, Exception, Transaction), the WebApi extension does not accept configuration options during registration. Customization is done via `IRestRulesProvider` implementation.

### Custom REST Rules

You can customize how RestMediator maps operations to HTTP responses by implementing `IRestRulesProvider`:

```csharp
using Minded.Extensions.WebApi;

public class CustomRestRulesProvider : IRestRulesProvider
{
    public IEnumerable<ICommandRestRule> CommandRules => new List<ICommandRestRule>
    {
        new CommandRestRule
        {
            Operation = RestOperation.Create,
            StatusCode = HttpStatusCode.Created,
            ContentResponse = ContentResponse.Result,
            RuleConditionProperty = new RuleConditionProperty
            {
                PropertyName = nameof(ICommandResponse.Successful),
                ExpectedValue = true
            }
        },
        new CommandRestRule
        {
            Operation = RestOperation.Update,
            StatusCode = HttpStatusCode.NoContent,
            ContentResponse = ContentResponse.None,
            RuleConditionProperty = new RuleConditionProperty
            {
                PropertyName = nameof(ICommandResponse.Successful),
                ExpectedValue = true
            }
        }
    };

    public IEnumerable<IQueryRestRule> QueryRules => new List<IQueryRestRule>
    {
        new QueryRestRule
        {
            Operation = RestOperation.GetSingle,
            StatusCode = HttpStatusCode.OK,
            ContentResponse = ContentResponse.Result,
            RuleConditionProperty = new RuleConditionProperty
            {
                PropertyName = "Result",
                ExpectedValue = null,
                Negate = true  // Result is NOT null
            }
        },
        new QueryRestRule
        {
            Operation = RestOperation.GetSingle,
            StatusCode = HttpStatusCode.NotFound,
            ContentResponse = ContentResponse.None,
            RuleConditionProperty = new RuleConditionProperty
            {
                PropertyName = "Result",
                ExpectedValue = null  // Result IS null
            }
        }
    };
}
```

Register your custom rules provider:

```csharp
services.AddScoped<IRestRulesProvider, CustomRestRulesProvider>();
```

### ContentResponse Options

The `ContentResponse` enum controls what gets returned in the HTTP response body:

```csharp
public enum ContentResponse
{
    None,    // No content in response body (204 No Content)
    Result,  // Only the result object
    Full     // Full response including metadata (Successful, OutcomeEntries, etc.)
}
```

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

### Custom Error Responses

Handle specific exceptions with custom HTTP status codes:

```csharp
public class UserNotFoundException : Exception
{
    public UserNotFoundException(int userId)
        : base($"User with ID {userId} not found") { }
}

// In your handler
public async Task<User> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken)
{
    var user = await _context.Users.FindAsync(query.UserId);

    if (user == null)
        throw new UserNotFoundException(query.UserId);

    return user;
}
```

### Returning Custom Status Codes

For operations that don't fit standard REST patterns, use custom rules:

```csharp
[HttpPost("archive/{id}")]
public async Task<IActionResult> Archive(int id)
{
    return await _mediator.ProcessRestCommandAsync(
        RestOperation.Custom,
        new ArchiveUserCommand(id));
}
```

## Integration with Other Decorators

### With Validation Decorator

```csharp
services.AddMinded(builder =>
{
    builder.AddCommandValidationDecorator();  // Validates before execution
    builder.AddCommandExceptionDecorator();   // Catches exceptions
    builder.AddCommandHandlers();
});

services.AddRestMediator();  // Maps validation errors to 400 Bad Request
```

### With Logging Decorator

```csharp
services.AddMinded(builder =>
{
    builder.AddCommandLoggingDecorator();     // Logs all operations
    builder.AddCommandExceptionDecorator();   // Catches and logs exceptions
    builder.AddCommandHandlers();
});

services.AddRestMediator();  // Returns appropriate HTTP codes
```

### With Caching Decorator

```csharp
services.AddMinded(builder =>
{
    builder.AddQueryMemoryCacheDecorator();   // Caches query results
    builder.AddQueryLoggingDecorator();       // Logs cache hits/misses
    builder.AddQueryHandlers();
});

services.AddRestMediator();  // Returns cached results with 200 OK
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
- **Delete** - Deleting a resource (204 No Content)

### 3. Validate Input with Model Binding

```csharp
[HttpPost]
public async Task<IActionResult> CreateUser(
    [FromBody] CreateUserCommand command,
    CancellationToken cancellationToken)
{
    // Model binding validates the command structure
    // FluentValidation decorator validates business rules
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

- **200 OK** - Successful GET, PUT, or DELETE with content
- **201 Created** - Successful POST
- **204 No Content** - Successful PUT or DELETE without content
- **400 Bad Request** - Validation errors
- **404 Not Found** - Resource not found
- **500 Internal Server Error** - Unhandled exceptions

## Troubleshooting

### Issue: Always Getting 500 Internal Server Error

**Cause**: Exception decorator not registered or exceptions not being caught.

**Solution**: Ensure exception decorator is registered:

```csharp
services.AddMinded(builder =>
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
services.AddMinded(builder =>
{
    builder.AddCommandValidationDecorator();
    builder.AddCommandHandlers();
});

[ValidateCommand]
public class CreateUserCommand : ICommand<User> { }
```

### Issue: Getting 404 for Existing Resources

**Cause**: Using `RestOperation.GetSingle` when result is null.

**Solution**: Ensure your handler returns the resource or throws an exception:

```csharp
public async Task<User> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken)
{
    var user = await _context.Users.FindAsync(query.UserId);

    if (user == null)
        throw new NotFoundException($"User {query.UserId} not found");

    return user;
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
