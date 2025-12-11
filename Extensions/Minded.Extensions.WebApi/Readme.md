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

## Best Practices

### 1. Use CancellationToken

Always pass the cancellation token to support request cancellation:

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(int id, CancellationToken cancellationToken)
{
    var query = new GetUserByIdQuery { UserId = id };
    return await _mediator.ProcessQueryAsync(query, cancellationToken);
}
```

### 2. Validate Input with Model Binding

```csharp
[HttpPost]
public async Task<IActionResult> CreateUser(
    [FromBody] CreateUserCommand command,
    CancellationToken cancellationToken)
{
    // Model binding validates the command
    // FluentValidation decorator validates business rules
    return await _mediator.ProcessCommandAsync(command, cancellationToken);
}
```

### 3. Follow RESTful Conventions

```csharp
// GET for queries
[HttpGet]
public async Task<IActionResult> GetUsers() { }

// POST for creating resources
[HttpPost]
public async Task<IActionResult> CreateUser() { }

// PUT for full updates
[HttpPut("{id}")]
public async Task<IActionResult> UpdateUser(int id) { }

// PATCH for partial updates
[HttpPatch("{id}")]
public async Task<IActionResult> PatchUser(int id) { }

// DELETE for deletions
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteUser(int id) { }
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.WebApi)
- [Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
