# Minded.Extensions.Validation.Abstractions

Validation abstractions including IValidator interface, validation attributes, and result types for the Minded Framework.

## Features

- **IValidator Interface** - Contract for implementing custom validators
- **Validation Attributes** - Attributes for marking validatable properties
- **Validation Result Types** - Standard validation result structures
- **Zero Dependencies** - Pure abstractions with no implementation dependencies

## Installation

```bash
dotnet add package Minded.Extensions.Validation.Abstractions
```

## Interfaces

### IValidator<T>

The core validation interface for implementing custom validators:

```csharp
public interface IValidator<in T>
{
    /// <summary>
    /// Validates the specified instance
    /// </summary>
    ValidationResult Validate(T instance);

    /// <summary>
    /// Validates the specified instance asynchronously
    /// </summary>
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
}
```

## Usage Examples

### Implementing a Custom Validator

```csharp
using Minded.Extensions.Validation.Abstractions;

public class CreateUserCommand
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public int Age { get; set; }
}

public class CreateUserCommandValidator : IValidator<CreateUserCommand>
{
    public ValidationResult Validate(CreateUserCommand instance)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(instance.Username))
            errors.Add(new ValidationError("Username", "Username is required"));
        else if (instance.Username.Length < 3)
            errors.Add(new ValidationError("Username", "Username must be at least 3 characters"));

        if (string.IsNullOrWhiteSpace(instance.Email))
            errors.Add(new ValidationError("Email", "Email is required"));
        else if (!IsValidEmail(instance.Email))
            errors.Add(new ValidationError("Email", "Email must be a valid email address"));

        if (string.IsNullOrWhiteSpace(instance.Password))
            errors.Add(new ValidationError("Password", "Password is required"));
        else if (instance.Password.Length < 8)
            errors.Add(new ValidationError("Password", "Password must be at least 8 characters"));

        if (instance.Age < 18)
            errors.Add(new ValidationError("Age", "User must be at least 18 years old"));

        return errors.Any()
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }

    public Task<ValidationResult> ValidateAsync(
        CreateUserCommand instance,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Validate(instance));
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
```

### Async Validation with Database Checks

```csharp
public class CreateUserCommandValidator : IValidator<CreateUserCommand>
{
    private readonly IUserRepository _userRepository;

    public CreateUserCommandValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public ValidationResult Validate(CreateUserCommand instance)
    {
        // Synchronous validation only
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(instance.Username))
            errors.Add(new ValidationError("Username", "Username is required"));

        return errors.Any()
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }

    public async Task<ValidationResult> ValidateAsync(
        CreateUserCommand instance,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();

        // Synchronous validations
        if (string.IsNullOrWhiteSpace(instance.Username))
        {
            errors.Add(new ValidationError("Username", "Username is required"));
        }
        else
        {
            // Async validation - check database
            var usernameExists = await _userRepository.UsernameExistsAsync(
                instance.Username,
                cancellationToken);

            if (usernameExists)
                errors.Add(new ValidationError("Username", "Username is already taken"));
        }

        if (!string.IsNullOrWhiteSpace(instance.Email))
        {
            var emailExists = await _userRepository.EmailExistsAsync(
                instance.Email,
                cancellationToken);

            if (emailExists)
                errors.Add(new ValidationError("Email", "Email is already registered"));
        }

        return errors.Any()
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }
}
```

### Validation Result

```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; }

    public static ValidationResult Success() => new ValidationResult
    {
        IsValid = true,
        Errors = new List<ValidationError>()
    };

    public static ValidationResult Failure(List<ValidationError> errors) => new ValidationResult
    {
        IsValid = false,
        Errors = errors
    };
}

public class ValidationError
{
    public string PropertyName { get; set; }
    public string ErrorMessage { get; set; }

    public ValidationError(string propertyName, string errorMessage)
    {
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
    }
}
```

## Best Practices

### 1. Separate Sync and Async Validation

```csharp
public ValidationResult Validate(CreateUserCommand instance)
{
    // Only synchronous validations here
    // Don't call async methods or .Result
}

public async Task<ValidationResult> ValidateAsync(...)
{
    // Both synchronous and asynchronous validations
    // Call Validate() first, then add async validations
}
```

### 2. Validate Early, Fail Fast

```csharp
public ValidationResult Validate(CreateUserCommand instance)
{
    var errors = new List<ValidationError>();

    // Check required fields first
    if (string.IsNullOrWhiteSpace(instance.Username))
    {
        errors.Add(new ValidationError("Username", "Username is required"));
        return ValidationResult.Failure(errors);  // Fail fast
    }

    // Then validate format/rules
    if (instance.Username.Length < 3)
        errors.Add(new ValidationError("Username", "Username must be at least 3 characters"));

    return errors.Any()
        ? ValidationResult.Failure(errors)
        : ValidationResult.Success();
}
```

### 3. Use Dependency Injection

```csharp
public class CreateUserCommandValidator : IValidator<CreateUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordPolicy _passwordPolicy;

    public CreateUserCommandValidator(
        IUserRepository userRepository,
        IPasswordPolicy passwordPolicy)
    {
        _userRepository = userRepository;
        _passwordPolicy = passwordPolicy;
    }

    // Use injected dependencies in validation
}
```

### 4. Provide Clear Error Messages

```csharp
// Good - clear, actionable messages
errors.Add(new ValidationError("Email", "Email must be a valid email address"));
errors.Add(new ValidationError("Password", "Password must be at least 8 characters and contain uppercase, lowercase, and numbers"));

// Avoid - vague messages
errors.Add(new ValidationError("Email", "Invalid"));
errors.Add(new ValidationError("Password", "Bad password"));
```

## Integration

This package provides only abstractions. For actual validation implementation, use:

- **Minded.Extensions.Validation** - FluentValidation integration
- **FluentValidation** - Powerful, fluent validation library

## Related Packages

- **Minded.Extensions.Validation** - FluentValidation integration and validation decorator
- **FluentValidation** - Validation library with fluent API

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.Validation.Abstractions)
- [Minded.Extensions.Validation](https://www.nuget.org/packages/Minded.Extensions.Validation)
- [FluentValidation](https://www.nuget.org/packages/FluentValidation)
- [Main Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
