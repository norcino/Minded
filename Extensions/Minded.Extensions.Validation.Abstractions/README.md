# Minded.Extensions.Validation.Abstractions

Validation abstractions for the Minded Framework: validator interfaces, the validation result contract and the opt-in attributes used by the validation decorators.

## Features

- **`IValidator<T>`** - Contract for reusable entity/object validators
- **`ICommandValidator<TCommand>` / `IQueryValidator<TQuery, TResult>`** - Contracts resolved by the validation decorators
- **`IValidationResult`** - Standard validation result contract with outcome entries and merging
- **`[ValidateCommand]` / `[ValidateQuery]`** - Opt-in attributes that activate the validation decorators
- **Minimal Dependencies** - Pure abstractions; depends only on the Minded CQRS abstractions

## Installation

```bash
dotnet add package Minded.Extensions.Validation.Abstractions
```

> This package contains only abstractions. The validation decorators, the concrete `ValidationResult` type and the DI registration methods (`AddCommandValidationDecorator()` / `AddQueryValidationDecorator()`) live in **Minded.Extensions.Validation**.

## Interfaces

### IValidator&lt;T&gt;

The generic validation contract for entities or any other class (namespace `Minded.Extensions.Validation`):

```csharp
public interface IValidator<in T> where T : class
{
    Task<IValidationResult> ValidateAsync(T subject);
}
```

### ICommandValidator&lt;TCommand&gt; and IQueryValidator&lt;TQuery, TResult&gt;

The contracts the validation decorators resolve from DI (namespace `Minded.Extensions.Validation.Decorator`):

```csharp
public interface ICommandValidator<TCommand> where TCommand : ICommand
{
    Task<IValidationResult> ValidateAsync(TCommand command);
}

public interface IQueryValidator<TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<IValidationResult> ValidateAsync(TQuery query);
}
```

### IValidationResult

```csharp
public interface IValidationResult
{
    // True when validation passed without errors (Warning and Info entries do not fail validation)
    bool IsValid { get; }

    // All outcome entries produced by the validation
    IList<IOutcomeEntry> OutcomeEntries { get; }

    // Merges two validation results; the merged result is invalid if either source is invalid
    IValidationResult Merge(IValidationResult validationResult);
}
```

Outcome entries are `IOutcomeEntry` instances from `Minded.Framework.CQRS.Abstractions`; the concrete `OutcomeEntry` type (shipped in the **Minded.Framework.CQRS** package, declared in the `Minded.Framework.CQRS.Abstractions` namespace) provides constructors `(propertyName, message)`, `(propertyName, message, attemptedValue)` and `(propertyName, message, attemptedValue, Severity severity, string errorCode)`. Severity values are `Error`, `Warning` and `Info`; the constructors without a severity parameter leave it at the enum default, `Error`, so such entries fail validation.

### Opt-in attributes

```csharp
[ValidateCommand]   // command is validated by its ICommandValidator<TCommand> before the handler runs
[ValidateQuery]     // query is validated by its IQueryValidator<TQuery, TResult> before the handler runs
```

The attributes are markers: the corresponding decorator (from **Minded.Extensions.Validation**) only validates commands/queries carrying the attribute. Every decorated command/query **must** have a registered validator — the decorator takes it as a constructor dependency, so a missing validator fails at dispatch time with a DI resolution error.

## Usage Examples

### Command validator

```csharp
using System.Threading.Tasks;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;

public class CreateUserCommandValidator : ICommandValidator<CreateUserCommand>
{
    private readonly IValidator<User> _userValidator;

    public CreateUserCommandValidator(IValidator<User> userValidator)
        => _userValidator = userValidator;

    public async Task<IValidationResult> ValidateAsync(CreateUserCommand command)
    {
        var result = new ValidationResult();   // concrete type from Minded.Extensions.Validation

        if (command.User == null)
        {
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(command.User), "{0} is required"));
            return result;   // early return - avoid null-reference cascade
        }

        // Reuse the entity validator and merge its outcome into this result
        return (await _userValidator.ValidateAsync(command.User)).Merge(result);
    }
}
```

### Entity validator

```csharp
public class UserValidator : IValidator<User>
{
    public Task<IValidationResult> ValidateAsync(User user)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(user.Username))
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(user.Username), "{0} is required",
                user.Username, Severity.Error));
        else if (user.Username.Length < 3)
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(user.Username), "{0} must be at least 3 characters",
                user.Username, Severity.Error));

        if (user.Age < 18)
            result.OutcomeEntries.Add(new OutcomeEntry(
                nameof(user.Age), "User must be at least 18 years old",
                user.Age, Severity.Error));

        return Task.FromResult<IValidationResult>(result);
    }
}
```

## Best Practices

### 1. Implement the Minded interfaces, use any validation library inside

The validation logic inside a validator may use any library (FluentValidation, DataAnnotations, plain code) — what matters is that the class implements `ICommandValidator<T>` / `IQueryValidator<TQuery, TResult>` / `IValidator<T>` so the decorator can resolve and invoke it.

### 2. Return early after null guards

```csharp
if (command.Entity == null)
{
    result.OutcomeEntries.Add(new OutcomeEntry(nameof(command.Entity), "{0} is required"));
    return result;   // avoid null-reference cascades in later rules
}
```

### 3. Reuse entity validators

Inject `IValidator<TEntity>` implementations into command validators and combine results with `Merge()` instead of duplicating rules.

### 4. Use severities deliberately

`Severity.Error` fails validation; `Severity.Warning` and `Severity.Info` are reported in the outcome entries without failing it.

### 5. Keep validators pure

Do not access the database or call `IMediator` from within a validator. Validators check the input; data-dependent checks belong to the handler or to dedicated guards.

### 6. Provide clear error messages

```csharp
// Good - clear, actionable messages
new OutcomeEntry(nameof(user.Email), "{0} must be a valid email address", user.Email, Severity.Error);

// Avoid - vague messages
new OutcomeEntry(nameof(user.Email), "Invalid", user.Email, Severity.Error);
```

## Integration

This package provides only abstractions. For the working pipeline, use:

- **Minded.Extensions.Validation** - validation decorators, concrete `ValidationResult`, validator auto-registration via `AddCommandValidationDecorator()` / `AddQueryValidationDecorator()`

## Related Packages

- **Minded.Extensions.Validation** - Validation decorators and concrete result types
- **Minded.Framework.CQRS.Abstractions** - `IOutcomeEntry`, `Severity` and the command/query contracts
- **Minded.Framework.CQRS** - Concrete `OutcomeEntry` implementation used in the examples above

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.Validation.Abstractions)
- [Minded.Extensions.Validation](https://www.nuget.org/packages/Minded.Extensions.Validation)
- [Main Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
