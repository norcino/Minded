# Minded.Extensions.Validation

Validation decorator for automatic command and query validation before handler execution.

## Features

- **Automatic Validation** - Validates commands and queries before they reach the handler
- **Bring Your Own Validation Library** - Implement the rules with FluentValidation, DataAnnotations, or plain code; the framework only requires that the validator class implements the Minded validator interfaces
- **Validation Result Aggregation** - Collects all validation outcomes and returns them in the response
- **Opt-in Behavior** - Validation runs only for commands/queries decorated with `[ValidateCommand]` / `[ValidateQuery]`
- **Outcome Tracking** - Validation outcomes are added to the response outcome entries

## Installation

```bash
dotnet add package Minded.Extensions.Validation
```

The validator interfaces (`ICommandValidator<TCommand>`, `IQueryValidator<TQuery, TResult>`, `IValidator<TEntity>`, `IValidationResult`) live in the `Minded.Extensions.Validation.Abstractions` package, which is referenced automatically.

## Quick Start

### 1. Create a Command

Decorate the command with `[ValidateCommand]` to opt in to validation:

```csharp
using Minded.Framework.CQRS.Command;
using Minded.Extensions.Validation.Decorator;

[ValidateCommand]
public class CreateUserCommand : ICommand<User>
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public int Age { get; set; }
}
```

### 2. Create a Validator

The validator class **must implement the Minded `ICommandValidator<TCommand>` interface** so the validation decorator can resolve it from DI. `ValidateAsync` returns a `Task<IValidationResult>`:

```csharp
using System.Threading.Tasks;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS;
using Minded.Framework.CQRS.Abstractions;

public class CreateUserCommandValidator : ICommandValidator<CreateUserCommand>
{
    public Task<IValidationResult> ValidateAsync(CreateUserCommand command)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(command.Username) || command.Username.Length < 3)
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(command.Username), "Username must be at least 3 characters",
                command.Username, Severity.Error));

        if (string.IsNullOrWhiteSpace(command.Email) || !command.Email.Contains("@"))
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(command.Email), "Email must be a valid email address",
                command.Email, Severity.Error));

        if (command.Age < 18)
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(command.Age), "User must be at least 18 years old",
                command.Age, Severity.Error));

        return Task.FromResult<IValidationResult>(result);
    }
}
```

The logic *inside* `ValidateAsync` can use any validation library you like (FluentValidation, DataAnnotations, plain code) — see [Using FluentValidation Internally](#using-fluentvalidation-internally). What matters is that the class implements the Minded interface.

### 3. Configure Validation Decorator

```csharp
using Minded.Extensions.Configuration;

services.AddMinded(configuration, mindedBuilderConfiguration: builder =>
{
    // Add validation decorator for commands
    builder.AddCommandValidationDecorator();

    // Add validation decorator for queries (optional)
    builder.AddQueryValidationDecorator();
});
```

`AddCommandValidationDecorator()` and `AddQueryValidationDecorator()` also **auto-register the validators**: they scan the service assemblies (selected by the `AddMinded` assembly filter) and register every implementation of `ICommandValidator<>`, `IQueryValidator<,>` and `IValidator<>` found there.

### 4. Automatic Validation

When a `[ValidateCommand]`-decorated command is dispatched, validation runs automatically before the handler. If validation fails, the handler is never invoked and the response carries the validation outcomes:

```csharp
var command = new CreateUserCommand
{
    Username = "ab",  // Too short
    Email = "invalid-email",  // Invalid format
    Age = 15  // Too young
};

var result = await _mediator.ProcessCommandAsync(command);

if (!result.Successful)
{
    // Validation failed - check outcome entries
    foreach (var outcome in result.OutcomeEntries)
    {
        Console.WriteLine($"{outcome.Severity}: {outcome.Message}");
    }

    // Output:
    // Error: Username must be at least 3 characters
    // Error: Email must be a valid email address
    // Error: User must be at least 18 years old
}
```

## Validator Interfaces

All three interfaces are defined in `Minded.Extensions.Validation.Abstractions` and follow the same shape — an async method returning `Task<IValidationResult>`:

| Interface | Validates | Method |
|-----------|-----------|--------|
| `ICommandValidator<TCommand>` | A command (resolved by the command validation decorator) | `Task<IValidationResult> ValidateAsync(TCommand command)` |
| `IQueryValidator<TQuery, TResult>` | A query (resolved by the query validation decorator) | `Task<IValidationResult> ValidateAsync(TQuery query)` |
| `IValidator<TEntity>` | A reusable entity/value, composed inside command/query validators | `Task<IValidationResult> ValidateAsync(TEntity subject)` |

## ValidationResult and Merge

`ValidationResult` is the built-in `IValidationResult` implementation:

- `IsValid` is `true` when no outcome entry has `Severity.Error` (warnings and info entries do **not** fail validation).
- `OutcomeEntries` is the list of `IOutcomeEntry` items produced by the validation.
- `Merge(IValidationResult)` appends the entries of another validation result to the current one and returns it — if either result contains errors, the merged result is invalid.

`Merge` makes it easy to compose a reusable entity validator into a command validator:

```csharp
public class CreateUserCommandValidator : ICommandValidator<CreateUserCommand>
{
    private readonly IValidator<User> _userValidator;

    public CreateUserCommandValidator(IValidator<User> userValidator)
        => _userValidator = userValidator;

    public async Task<IValidationResult> ValidateAsync(CreateUserCommand command)
    {
        var result = new ValidationResult();

        if (command.User == null)
        {
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(command.User), "User is required", null, Severity.Error));
            return result;  // Early return - avoid null-reference cascade
        }

        // Merge the entity validator's entries into the command validator's result
        return (await _userValidator.ValidateAsync(command.User)).Merge(result);
    }
}

public class UserValidator : IValidator<User>
{
    public Task<IValidationResult> ValidateAsync(User user)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(user.Name))
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(user.Name), "Name cannot be empty", user.Name, Severity.Error));

        return Task.FromResult<IValidationResult>(result);
    }
}
```

## Advanced Usage

### Async Validation

`ValidateAsync` is naturally asynchronous, so validators can await I/O such as uniqueness checks:

```csharp
public class CreateUserCommandValidator : ICommandValidator<CreateUserCommand>
{
    private readonly IUserRepository _userRepository;

    public CreateUserCommandValidator(IUserRepository userRepository)
        => _userRepository = userRepository;

    public async Task<IValidationResult> ValidateAsync(CreateUserCommand command)
    {
        var result = new ValidationResult();

        if (await _userRepository.UsernameExistsAsync(command.Username))
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(command.Username), "Username is already taken",
                command.Username, Severity.Error));

        if (await _userRepository.EmailExistsAsync(command.Email))
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(command.Email), "Email is already registered",
                command.Email, Severity.Error));

        return result;
    }
}
```

### Using FluentValidation Internally

FluentValidation (or any other library) can implement the rules **inside** the Minded validator. The decorator only resolves and invokes the Minded interface; how the result is produced is an internal detail of your validator class:

```csharp
using FluentValidation;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;

public class CreateUserCommandValidator : ICommandValidator<CreateUserCommand>
{
    // Internal FluentValidation rules - not visible to the framework
    private class Rules : AbstractValidator<CreateUserCommand>
    {
        public Rules()
        {
            RuleFor(x => x.Username).NotEmpty().MinimumLength(3);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Age).GreaterThanOrEqualTo(18);
        }
    }

    public async Task<IValidationResult> ValidateAsync(CreateUserCommand command)
    {
        var fluentResult = await new Rules().ValidateAsync(command);

        // Map FluentValidation failures to Minded outcome entries
        return new ValidationResult(fluentResult.Errors.Select(f =>
            (IOutcomeEntry)new OutcomeEntry(
                f.PropertyName, f.ErrorMessage, f.AttemptedValue, Severity.Error)));
    }
}
```

> A plain `AbstractValidator<T>` on its own is **not** picked up by the framework — the decorator resolves `ICommandValidator<TCommand>` / `IQueryValidator<TQuery, TResult>` from DI, so the class registered for the command/query must implement the Minded interface.

### Outcome Severity

Use `Severity` to distinguish hard failures from advisory checks. Only `Severity.Error` entries make a result invalid; `Warning` and `Info` entries flow through to the response without blocking the handler:

```csharp
result.OutcomeEntries.Add(new OutcomeEntry(
    nameof(command.Password), "Password is weak",
    null, Severity.Warning, "WeakPassword"));
```

## Best Practices

### 1. Keep Validators Focused

Each validator should validate a single command or query:

```csharp
// Good - focused validator
public class CreateUserCommandValidator : ICommandValidator<CreateUserCommand> { }

// Avoid - generic validators that try to handle multiple types
```

### 2. Use Meaningful Error Messages

```csharp
// Good - clear, actionable messages
new OutcomeEntry(nameof(command.Email), "Email must be a valid email address", command.Email, Severity.Error)

// Avoid - generic messages
new OutcomeEntry(nameof(command.Email), "Invalid", command.Email, Severity.Error)
```

### 3. Validate Business Rules in Validators

```csharp
public class TransferFundsCommandValidator : ICommandValidator<TransferFundsCommand>
{
    public Task<IValidationResult> ValidateAsync(TransferFundsCommand command)
    {
        var result = new ValidationResult();

        if (command.Amount <= 0)
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(command.Amount), "Transfer amount must be greater than zero",
                command.Amount, Severity.Error));

        if (command.FromAccountId == command.ToAccountId)
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(command.ToAccountId), "Cannot transfer to the same account",
                command.ToAccountId, Severity.Error));

        return Task.FromResult<IValidationResult>(result);
    }
}
```

### 4. Reuse Entity Validators

Inject `IValidator<TEntity>` implementations into command validators and combine the results with `Merge` (see [ValidationResult and Merge](#validationresult-and-merge)).

## Configuration Options

### Decorator Registration

The validation decorator has **no runtime configuration options**. Validation behavior is controlled entirely by:

1. **Attributes** - `[ValidateCommand]` or `[ValidateQuery]` on commands/queries
2. **Validators** - classes implementing the Minded validator interfaces

```csharp
// Register validation decorator for commands
builder.AddCommandValidationDecorator();

// Register validation decorator for queries
builder.AddQueryValidationDecorator();
```

**Note:** Unlike other decorators (Logging, Exception, Transaction), the Validation decorator does not accept configuration options during registration. All validation logic is defined in your validator classes.

### Controlling Validation Behavior

#### 1. Attributes (Required)

Commands/queries must be decorated with the appropriate attribute, otherwise the decorator is not applied:

```csharp
[ValidateCommand]  // For commands
public class CreateUserCommand : ICommand<User> { }

[ValidateQuery]    // For queries
public class GetUserQuery : IQuery<User> { }
```

#### 2. Validator Classes (Required for decorated types)

Every `[ValidateCommand]`-decorated command **must** have a registered `ICommandValidator<TCommand>`, and every `[ValidateQuery]`-decorated query an `IQueryValidator<TQuery, TResult>`. The decorator takes the validator as a **constructor dependency**, so a decorated command/query with no registered validator fails at dispatch time with a DI resolution error (see [Troubleshooting](#troubleshooting)).

## Integration with RestMediator

When using with `Minded.Extensions.WebApi`, validation errors automatically return HTTP 400 Bad Request:

```csharp
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IRestMediator _mediator;

    public UserController(IRestMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
    {
        // Validation runs automatically
        // Returns 400 Bad Request if validation fails
        // Returns 200 OK with result if validation passes
        return await _mediator.ProcessRestCommandAsync(RestOperation.CreateWithContent, command);
    }
}
```

## Troubleshooting

### DI Resolution Error When Dispatching

If dispatching a `[ValidateCommand]`-decorated command throws a dependency-injection resolution error (unable to resolve `ICommandValidator<TCommand>`), the command has no registered validator. The validation decorator receives the validator through its constructor, so the failure happens at dispatch time, not at startup.

Validators are auto-registered by `AddCommandValidationDecorator()` / `AddQueryValidationDecorator()`, which scan the service assemblies selected by the `AddMinded` assembly filter. Ensure:

1. The validator class implements `ICommandValidator<TCommand>` (or `IQueryValidator<TQuery, TResult>`), and
2. It lives in an assembly matched by the `AddMinded` assembly filter.

### Validation Errors Not Appearing

Check that the validation decorator is registered **before** other decorators (first registered = innermost, runs right before the handler):

```csharp
services.AddMinded(configuration, mindedBuilderConfiguration: builder =>
{
    builder.AddCommandValidationDecorator();  // First registered = innermost
    builder.AddCommandLoggingDecorator();     // After validation
    builder.AddCommandExceptionDecorator();   // Last registered = outermost
});
```

## Integration with Other Decorators

### With Exception Decorator

The Exception decorator should wrap the Validation decorator to catch any validation errors:

```csharp
builder.AddCommandValidationDecorator()
       .AddCommandExceptionDecorator()  // Catches validation exceptions
       .AddCommandHandlers();
```

See: [Exception Decorator Documentation](../Minded.Extensions.Exception/README.md)

### With Logging Decorator

The Logging decorator logs validation failures:

```csharp
builder.AddCommandValidationDecorator()
       .AddCommandLoggingDecorator()    // Logs validation results
       .AddCommandExceptionDecorator()
       .AddCommandHandlers();
```

See: [Logging Decorator Documentation](../Minded.Extensions.Logging/README.md)

### With RestMediator

RestMediator automatically maps validation errors to HTTP 400 Bad Request:

```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateUserCommand command)
{
    // Validation errors automatically return 400 Bad Request
    return await _restMediator.ProcessRestCommandAsync(
        RestOperation.CreateWithContent,
        command);
}
```

See: [RestMediator Documentation](../Minded.Extensions.WebApi/README.md)

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.Validation)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/) (optional library for implementing rules inside Minded validators)
- [Main Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
