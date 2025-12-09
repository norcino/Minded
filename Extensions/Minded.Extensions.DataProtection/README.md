# Minded.Extensions.DataProtection

Default implementation of data protection and sensitive data sanitization for the Minded Framework.

## Overview

This package provides the default implementation of `IDataSanitizer` that automatically protects sensitive data (PII, confidential business data) in logs, exception messages, and other output. It works seamlessly with Logging and Exception decorators.

## Features

- **Automatic Sanitization** - Reflection-based inspection of objects
- **Attribute-Based Protection** - Uses `[SensitiveData]` attribute to identify sensitive properties
- **Configurable Behavior** - Show or hide sensitive data based on environment, feature flags, etc.
- **Performance Optimized** - Recursion depth limiting and collection truncation
- **Thread-Safe** - Can be registered as a singleton
- **Optional Dependency** - Logging and Exception decorators work without this package (using no-op implementation)

## Installation

```bash
dotnet add package Minded.Extensions.DataProtection
dotnet add package Minded.Extensions.DataProtection.Abstractions
```

## Quick Start

### 1. Mark Sensitive Properties

```csharp
using Minded.Extensions.DataProtection.Abstractions;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    
    [SensitiveData]
    public string Email { get; set; }
    
    [SensitiveData]
    public string Password { get; set; }
}
```

### 2. Configure Data Protection

```csharp
using Minded.Extensions.Configuration;
using Minded.Extensions.DataProtection;

services.AddMinded(builder =>
{
    // Add data protection with default settings
    builder.AddDataProtection();
    
    // Now add logging and/or exception decorators
    builder.AddCommandLoggingDecorator();
    builder.AddCommandExceptionDecorator();
});
```

### 3. Configure Sensitive Data Visibility

```csharp
services.AddMinded(builder =>
{
    builder.AddDataProtection(options =>
    {
        // Option 1: Static configuration
        options.ShowSensitiveData = false; // Default: hide

        // Option 2: Dynamic based on environment
        options.ShowSensitiveDataProvider = () => _environment.IsDevelopment();

        // Option 3: Dynamic based on feature flag
        options.ShowSensitiveDataProvider = () => _featureFlags.IsEnabled("ShowSensitiveData");
    });
});
```

## Configuration Options

### DataProtectionOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ShowSensitiveData` | `bool` | `false` | Static setting for showing/hiding sensitive data |
| `ShowSensitiveDataProvider` | `Func<bool>` | `null` | Dynamic provider for runtime configuration (takes precedence) |

### Configuration File

```json
{
  "Minded": {
    "DataProtectionOptions": {
      "ShowSensitiveData": false
    }
  }
}
```

## Custom Implementation

You can provide your own `IDataSanitizer` implementation:

```csharp
public class MyCustomSanitizer : IDataSanitizer
{
    public IDictionary<string, object> Sanitize(object obj)
    {
        // Your custom logic (e.g., encryption instead of omission)
    }
    
    // Implement other interface members...
}

// Register custom implementation
services.AddMinded(builder =>
{
    builder.AddDataProtection<MyCustomSanitizer>();
});
```

## How It Works

### With DataProtection Configured

```csharp
var user = new User 
{ 
    Id = 1, 
    Username = "john", 
    Email = "john@example.com",  // [SensitiveData]
    Password = "secret123"        // [SensitiveData]
};

// When ShowSensitiveData = false (default):
// Output: { "Id": 1, "Username": "john" }
// Email and Password are omitted

// When ShowSensitiveData = true:
// Output: { "Id": 1, "Username": "john", "Email": "john@example.com", "Password": "secret123" }
```

### Without DataProtection Configured

If you don't call `AddDataProtection()`, the Logging and Exception decorators will use a no-op implementation (`NullDataSanitizer`) that passes data through unchanged. This allows you to use those decorators without requiring DataProtection.

## Best Practices

1. **Always mark sensitive properties** - Use `[SensitiveData]` on PII and confidential data
2. **Hide by default** - Keep `ShowSensitiveData = false` in production
3. **Use providers for dynamic config** - Leverage `ShowSensitiveDataProvider` for environment-based settings
4. **Test in development** - Enable `ShowSensitiveData` in dev to verify logging works correctly
5. **Review regularly** - Audit your entities to ensure all sensitive properties are marked

## Performance Considerations

- **Recursion Depth**: Limited to 3 levels to prevent infinite loops
- **Collection Truncation**: Collections are limited to 10 items in output
- **Reflection Caching**: Consider implementing caching for high-throughput scenarios

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [Documentation](https://github.com/norcino/Minded)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)

