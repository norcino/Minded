# Minded.Extensions.DataProtection.Abstractions

Core abstractions for data protection and sensitive data handling in the Minded Framework.

## Overview

This package provides the foundational interfaces and attributes for protecting sensitive data (PII, confidential business data) in logs, exception messages, and other output. It contains no implementation - only abstractions that other packages can depend on.

## Features

- **`[SensitiveData]` Attribute** - Mark properties containing sensitive information
- **`IDataSanitizer` Interface** - Contract for sanitizing objects before output
- **`DataProtectionOptions`** - Configuration for controlling sensitive data visibility
- **Zero Dependencies** - Pure abstractions with no implementation dependencies

## Installation

```bash
dotnet add package Minded.Extensions.DataProtection.Abstractions
```

## Usage

### Marking Sensitive Properties

```csharp
using Minded.Extensions.DataProtection.Abstractions;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    
    [SensitiveData]
    public string Email { get; set; }
    
    [SensitiveData]
    public string PhoneNumber { get; set; }
    
    [SensitiveData]
    public string SSN { get; set; }
}
```

### Implementing IDataSanitizer

```csharp
using Minded.Extensions.DataProtection.Abstractions;

public class MyCustomSanitizer : IDataSanitizer
{
    private readonly IOptions<DataProtectionOptions> _options;
    
    public MyCustomSanitizer(IOptions<DataProtectionOptions> options)
    {
        _options = options;
    }
    
    public IDictionary<string, object> Sanitize(object obj)
    {
        // Your custom sanitization logic
        // Check _options.Value.GetEffectiveShowSensitiveData()
        // to determine whether to show or hide sensitive properties
    }
    
    // Implement other interface members...
}
```

## Related Packages

- **Minded.Extensions.DataProtection** - Default implementation of IDataSanitizer
- **Minded.Extensions.Logging** - Uses IDataSanitizer for log sanitization
- **Minded.Extensions.Exception** - Uses IDataSanitizer for exception message sanitization

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [Documentation](https://github.com/norcino/Minded)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)

