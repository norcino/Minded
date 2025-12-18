# Minded.Extensions.Validation

Validation decorator for automatic command and query validation before handler execution using FluentValidation.

## Features

- **Automatic Validation** - Validates commands and queries before they reach the handler
- **FluentValidation Integration** - Uses FluentValidation for powerful, fluent validation rules
- **Validation Result Aggregation** - Collects all validation errors and returns them in the response
- **Configurable Behavior** - Control validation execution per command/query
- **Outcome Tracking** - Validation errors are added to the response outcomes

## Installation

```bash
dotnet add package Minded.Extensions.Validation
dotnet add package FluentValidation
```

## Quick Start

### 1. Create a Command

```csharp
using Minded.Framework.CQRS.Abstractions;

public class CreateUserCommand : ICommand<User>
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public int Age { get; set; }
}
```

### 2. Create a Validator

```csharp
using FluentValidation;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters")
            .MaximumLength(50).WithMessage("Username cannot exceed 50 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number");

        RuleFor(x => x.Age)
            .GreaterThanOrEqualTo(18).WithMessage("User must be at least 18 years old")
            .LessThan(120).WithMessage("Age must be less than 120");
    }
}
```

### 3. Configure Validation Decorator

```csharp
using Minded.Extensions.Configuration;

services.AddMinded(builder =>
{
    // Add validation decorator for commands
    builder.AddCommandValidationDecorator();

    // Add validation decorator for queries (optional)
    builder.AddQueryValidationDecorator();
});
```

### 4. Automatic Validation

When a command is dispatched, validation runs automatically:

```csharp
var command = new CreateUserCommand
{
    Username = "ab",  // Too short
    Email = "invalid-email",  // Invalid format
    Password = "weak",  // Doesn't meet requirements
    Age = 15  // Too young
};

var result = await _mediator.ProcessCommandAsync(command);

if (!result.Success)
{
    // Validation failed - check outcomes
    foreach (var outcome in result.Outcomes)
    {
        Console.WriteLine($"{outcome.Severity}: {outcome.Message}");
    }

    // Output:
    // Error: Username must be at least 3 characters
    // Error: Email must be a valid email address
    // Error: Password must be at least 8 characters
    // Error: Password must contain at least one uppercase letter
    // Error: Password must contain at least one number
    // Error: User must be at least 18 years old
}
```

## Advanced Usage

### Complex Validation Rules

```csharp
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required")
            .GreaterThan(0).WithMessage("Customer ID must be positive");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item")
            .Must(items => items.Count <= 100).WithMessage("Order cannot contain more than 100 items");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("Product ID is required");

            item.RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0")
                .LessThanOrEqualTo(1000).WithMessage("Quantity cannot exceed 1000");

            item.RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0");
        });

        RuleFor(x => x.ShippingAddress)
            .NotNull().WithMessage("Shipping address is required")
            .SetValidator(new AddressValidator());
    }
}

public class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(x => x.Street).NotEmpty().WithMessage("Street is required");
        RuleFor(x => x.City).NotEmpty().WithMessage("City is required");
        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("Postal code is required")
            .Matches(@"^\d{5}(-\d{4})?$").WithMessage("Invalid postal code format");
    }
}
```

### Async Validation

```csharp
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    private readonly IUserRepository _userRepository;

    public CreateUserCommandValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;

        RuleFor(x => x.Username)
            .NotEmpty()
            .MustAsync(BeUniqueUsername).WithMessage("Username is already taken");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(BeUniqueEmail).WithMessage("Email is already registered");
    }

    private async Task<bool> BeUniqueUsername(string username, CancellationToken cancellationToken)
    {
        return !await _userRepository.UsernameExistsAsync(username, cancellationToken);
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        return !await _userRepository.EmailExistsAsync(email, cancellationToken);
    }
}
```

## Best Practices

### 1. Keep Validators Focused

Each validator should validate a single command or query:

```csharp
// Good - focused validator
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand> { }

// Avoid - generic validators that try to handle multiple types
```

### 2. Use Meaningful Error Messages

```csharp
// Good - clear, actionable messages
RuleFor(x => x.Email)
    .EmailAddress().WithMessage("Email must be a valid email address");

// Avoid - generic messages
RuleFor(x => x.Email)
    .EmailAddress().WithMessage("Invalid");
```

### 3. Validate Business Rules in Validators

```csharp
public class TransferFundsCommandValidator : AbstractValidator<TransferFundsCommand>
{
    public TransferFundsCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Transfer amount must be greater than zero")
            .LessThanOrEqualTo(10000).WithMessage("Transfer amount cannot exceed $10,000 per transaction");

        RuleFor(x => x.FromAccountId)
            .NotEqual(x => x.ToAccountId).WithMessage("Cannot transfer to the same account");
    }
}
```

### 4. Register Validators in DI Container

Validators are automatically discovered and registered when using `AddMinded()`:

```csharp
services.AddMinded(builder =>
{
    builder.AddCommandValidationDecorator();
});

// Validators are automatically registered as:
// services.AddTransient<IValidator<CreateUserCommand>, CreateUserCommandValidator>();
```

## Configuration Options

### Decorator Registration

The validation decorator has **no runtime configuration options**. Validation behavior is controlled entirely by:

1. **Attributes** - `[ValidateCommand]` or `[ValidateQuery]` on commands/queries
2. **Validators** - FluentValidation validator classes

```csharp
// Register validation decorator for commands
builder.AddCommandValidationDecorator();

// Register validation decorator for queries
builder.AddQueryValidationDecorator();

// Register both
builder.AddCommandValidationDecorator();
builder.AddQueryValidationDecorator();
```

**Note:** Unlike other decorators (Logging, Exception, Transaction), the Validation decorator does not accept configuration options during registration. All validation logic is defined in your FluentValidation validator classes.

### Controlling Validation Behavior

Validation behavior is controlled through:

#### 1. Attributes (Required)

Commands/queries must be decorated with the appropriate attribute:

```csharp
[ValidateCommand]  // For commands
public class CreateUserCommand : ICommand<User> { }

[ValidateQuery]    // For queries
public class GetUserQuery : IQuery<User> { }
```

#### 2. Validator Classes

Define validation rules using FluentValidation:

```csharp
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Age).GreaterThanOrEqualTo(18);
    }
}
```

#### 3. Validator Severity

Control outcome severity in validators:

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .WithSeverity(Severity.Error);  // Error, Warning, or Information
```

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
        return await _mediator.ProcessCommandAsync(command);
    }
}
```

## Troubleshooting

### Validators Not Running

Ensure validators are in the same assembly as your commands/queries, or explicitly register them:

```csharp
services.AddValidatorsFromAssembly(typeof(CreateUserCommandValidator).Assembly);
```

### Validation Errors Not Appearing

Check that the validation decorator is registered **before** other decorators:

```csharp
services.AddMinded(builder =>
{
    builder.AddCommandValidationDecorator();  // First
    builder.AddCommandLoggingDecorator();     // After validation
    builder.AddCommandExceptionDecorator();   // Last
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
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [Main Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
