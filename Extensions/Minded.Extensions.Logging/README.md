# Minded.Extensions.Logging

Logging decorator for automatic request/response logging with configurable detail levels, outcome tracking, and optional sensitive data protection.

## Features

- Request and response logging
- Outcome entry logging with severity filtering
- **Optional sensitive data protection** - Automatically hide PII and confidential data from logs (requires `Minded.Extensions.DataProtection`)
- Dynamic configuration via providers (feature flags support)
- Template data logging control
- GDPR/CCPA compliance support

## Installation

```bash
dotnet add package Minded.Extensions.Logging
```

For sensitive data protection, also install:

```bash
dotnet add package Minded.Extensions.DataProtection
```

## Quick Start

### Basic Usage (Without Data Protection)

```csharp
services.AddMinded(Configuration, assembly => assembly.Name.StartsWith("Service."), builder =>
{
    builder.AddLogging();
});
```

### With Sensitive Data Protection

#### 1. Mark Sensitive Properties

Use the `[SensitiveData]` attribute to mark properties containing PII or confidential business data:

```csharp
using Minded.Extensions.DataProtection.Abstractions;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }

    [SensitiveData]  // Will be omitted from logs by default
    public string Email { get; set; }

    [SensitiveData]  // Will be omitted from logs by default
    public string Surname { get; set; }
}

public class CreateUserCommand : ICommand<User>
{
    public User User { get; set; }
}
```

#### 2. Configure DataProtection

By default, sensitive data is **hidden** for security and compliance. You can enable it for development:

```csharp
services.AddMinded(Configuration, assembly => assembly.Name.StartsWith("Service."), builder =>
{
    // Configure DataProtection
    builder.AddDataProtection(options =>
    {
        // Option 1: Static configuration
        options.ShowSensitiveData = false;  // Default: hide sensitive data

        // Option 2: Dynamic configuration (recommended)
        options.ShowSensitiveDataProvider = () => _environment.IsDevelopment();
    });

    // Add logging (will use DataProtection automatically)
    builder.AddLogging();
});
```

#### 3. Automatic Protection

When a command or query is logged, sensitive properties are automatically omitted:

```csharp
// Without [SensitiveData] - all properties logged:
{
    "Id": 123,
    "Name": "John",
    "Email": "john.doe@example.com",
    "Surname": "Doe"
}

// With [SensitiveData] and ShowSensitiveData = false (default):
{
    "Id": 123,
    "Name": "John"
    // Email and Surname are omitted
}
```

## Configuration Options

### DataProtection Options

See [Minded.Extensions.DataProtection](../Minded.Extensions.DataProtection/README.md) for complete DataProtection configuration options.

### Logging Options

#### ShowSensitiveData (bool) - DEPRECATED

**Note**: This option has been moved to `DataProtectionOptions`. For backward compatibility, it still exists in `LoggingOptions` but is deprecated.

Use `DataProtectionOptions.ShowSensitiveData` instead:

```csharp
builder.AddDataProtection(options =>
{
    options.ShowSensitiveData = false;  // Hide sensitive data (default)
});
```

#### ShowSensitiveDataProvider (Func<bool>) - DEPRECATED

**Note**: This option has been moved to `DataProtectionOptions`. For backward compatibility, it still exists in `LoggingOptions` but is deprecated.

Use `DataProtectionOptions.ShowSensitiveDataProvider` instead:

Dynamic provider for runtime configuration. Takes precedence over `ShowSensitiveData`.

```csharp
// Show sensitive data only in development environment
options.ShowSensitiveDataProvider = () => _environment.IsDevelopment();

// Show sensitive data based on feature flag
options.ShowSensitiveDataProvider = () => _featureFlags.IsEnabled("ShowSensitiveDataInLogs");

// Show sensitive data based on configuration
options.ShowSensitiveDataProvider = () => _configuration.GetValue<bool>("Logging:ShowSensitiveData");
```

## Best Practices

### 1. Mark All Sensitive Properties

Always mark properties containing personal or confidential data:

```csharp
public class Customer
{
    public int Id { get; set; }
    public string CompanyName { get; set; }

    [SensitiveData]
    public string Email { get; set; }

    [SensitiveData]
    public string PhoneNumber { get; set; }

    [SensitiveData]
    public string TaxId { get; set; }

    [SensitiveData]
    public string CreditCardNumber { get; set; }
}
```

### 2. Use Provider Pattern for Environment-Specific Configuration

```csharp
services.AddMinded(builder =>
{
    builder.AddLogging(options =>
    {
        // Show sensitive data in development, hide in production
        options.ShowSensitiveDataProvider = () => _environment.IsDevelopment();

        // Or use configuration
        options.ShowSensitiveDataProvider = () =>
            _configuration.GetValue<bool>("Logging:ShowSensitiveData", false);
    });
});
```

### 3. Never Log Sensitive Data in Production

Keep `ShowSensitiveData = false` in production to comply with:
- **GDPR** (General Data Protection Regulation)
- **CCPA** (California Consumer Privacy Act)
- **PCI DSS** (Payment Card Industry Data Security Standard)
- **HIPAA** (Health Insurance Portability and Accountability Act)

### 4. Nested Objects Are Supported

The data sanitizer recursively inspects nested objects:

```csharp
public class Order
{
    public int Id { get; set; }
    public decimal Total { get; set; }

    public Customer Customer { get; set; }  // Customer.Email will be hidden
    public List<OrderItem> Items { get; set; }
}
```

## Advanced Features

### Collection Truncation

Collections are automatically truncated to 10 items in logs to prevent excessive log size:

```csharp
// If Items has 100 elements, only first 10 will be logged
public class Order
{
    public List<OrderItem> Items { get; set; }  // Max 10 items in logs
}
```

### Recursion Protection

The sanitizer has a maximum depth of 3 levels to prevent infinite recursion:

```csharp
public class Node
{
    public int Id { get; set; }
    public Node Parent { get; set; }  // Will be inspected up to 3 levels deep
    public List<Node> Children { get; set; }
}
```

## Usage

See the [main documentation](https://github.com/norcino/Minded) for comprehensive usage examples.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.Logging)
- [Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
