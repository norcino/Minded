# Minded.Extensions.Exception

Exception handling decorator with automatic error logging, graceful failure handling, and optional sensitive data protection.

## Features

- Automatic exception catching and logging
- OperationCanceledException special handling
- Configurable error responses
- Integration with Microsoft.Extensions.Logging
- **Optional sensitive data protection** - Automatically hide PII and confidential data from exception logs (requires `Minded.Extensions.DataProtection`)

## Installation

```bash
dotnet add package Minded.Extensions.Exception
```

For sensitive data protection in exception logs, also install:

```bash
dotnet add package Minded.Extensions.DataProtection
```

## Usage

### Basic Usage (Without Data Protection)

```csharp
services.AddMinded(Configuration, assembly => assembly.Name.StartsWith("Service."), builder =>
{
    builder.AddCommandExceptionDecorator();
    builder.AddQueryExceptionDecorator();
});
```

### With Sensitive Data Protection

When DataProtection is configured, exception logs will automatically sanitize sensitive data:

```csharp
using Minded.Extensions.DataProtection.Abstractions;

// Mark sensitive properties
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }

    [SensitiveData]  // Will be omitted from exception logs
    public string Email { get; set; }
}

// Configure DataProtection and Exception handling
services.AddMinded(Configuration, assembly => assembly.Name.StartsWith("Service."), builder =>
{
    builder.AddDataProtection(options =>
    {
        options.ShowSensitiveDataProvider = () => _environment.IsDevelopment();
    });

    builder.AddCommandExceptionDecorator();
    builder.AddQueryExceptionDecorator();
});
```

For more details, see the [main documentation](https://github.com/norcino/Minded) for comprehensive usage examples.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.Exception)
- [Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
