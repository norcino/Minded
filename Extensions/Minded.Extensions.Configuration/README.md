# Minded.Extensions.Configuration

Configuration infrastructure for Minded Framework including MindedBuilder for fluent decorator registration and service collection extensions.

## Features

- **Fluent Decorator Registration** - Chain decorator configuration with a clean, readable API
- **Service Collection Extensions** - Easy integration with ASP.NET Core dependency injection
- **Assembly Scanning** - Automatic discovery and registration of handlers and validators
- **Configuration Options** - Support for IConfiguration and options pattern
- **Decorator Ordering** - Control the order of decorator execution

## Installation

```bash
dotnet add package Minded.Extensions.Configuration
```

## Quick Start

### Basic Configuration

```csharp
using Minded.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMinded(builder =>
        {
            // Fluent API for decorator registration
            builder.AddCommandValidationDecorator();
            builder.AddCommandLoggingDecorator();
            builder.AddCommandExceptionDecorator();

            builder.AddQueryValidationDecorator();
            builder.AddQueryLoggingDecorator();
            builder.AddQueryExceptionDecorator();
        });
    }
}
```

### Configuration with Assembly Scanning

```csharp
services.AddMinded(
    configuration: Configuration,
    assemblyFilter: assembly => assembly.Name.StartsWith("MyApp."),
    configure: builder =>
    {
        builder.AddCommandValidationDecorator();
        builder.AddCommandLoggingDecorator();
    });
```

### Configuration with Options

```csharp
services.AddMinded(builder =>
{
    // Configure with options
    builder.AddDataProtection(options =>
    {
        options.ShowSensitiveData = false;
    });

    builder.AddCommandLoggingDecorator();
});
```

## MindedBuilder API

The `MindedBuilder` provides a fluent API for configuring the Minded framework:

### Command Decorators

```csharp
builder.AddCommandValidationDecorator();      // Validation
builder.AddCommandLoggingDecorator();         // Logging
builder.AddCommandExceptionDecorator();       // Exception handling
builder.AddCommandRetryDecorator();           // Retry logic
builder.AddCommandCachingDecorator();         // Caching (not typical for commands)
```

### Query Decorators

```csharp
builder.AddQueryValidationDecorator();        // Validation
builder.AddQueryLoggingDecorator();           // Logging
builder.AddQueryExceptionDecorator();         // Exception handling
builder.AddQueryRetryDecorator();             // Retry logic
builder.AddQueryCachingDecorator();           // Caching
```

### Data Protection

```csharp
builder.AddDataProtection();                  // Default configuration
builder.AddDataProtection(options => { });    // With options
builder.AddDataProtection<MyCustomSanitizer>(); // Custom implementation
```

## Assembly Scanning

The configuration system automatically scans assemblies to register:

- **Command Handlers** - Classes implementing `ICommandHandler<TCommand, TResult>`
- **Query Handlers** - Classes implementing `IQueryHandler<TQuery, TResult>`
- **Decorators support classes** - Classes implementing `IValidator<T>` or other interfaces listed as required by extension decorators

### Default Scanning

```csharp
// Scans all assemblies in the application domain
services.AddMinded(builder => { });
```

### Filtered Scanning

```csharp
// Scan only assemblies matching the filter
services.AddMinded(
    assemblyFilter: assembly => assembly.Name.StartsWith("MyApp."),
    configure: builder => { });
```

### Explicit Assembly Registration

```csharp
// Register handlers from specific assemblies
services.AddMinded(builder =>
{
    builder.RegisterHandlersFromAssembly(typeof(CreateUserCommand).Assembly);
    builder.RegisterValidatorsFromAssembly(typeof(CreateUserCommandValidator).Assembly);
});
```

## Decorator Order

Decorators are executed in the order they are registered. The recommended order is:

```csharp
services.AddMinded(builder =>
{
    // 1. Exception handling (outermost - catches all exceptions)
    builder.AddCommandExceptionDecorator();

    // 2. Logging (logs all requests/responses)
    builder.AddCommandLoggingDecorator();

    // 3. Validation (validates before processing)
    builder.AddCommandValidationDecorator();

    // 4. Retry (retries on transient failures)
    builder.AddCommandRetryDecorator();

    // 5. Caching (innermost - caches results)
    builder.AddCommandCachingDecorator();

    // Handler executes last
});
```

## Configuration Options

### Using IConfiguration

```csharp
public class Startup
{
    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMinded(Configuration, builder =>
        {
            // Configuration is available to decorators
            builder.AddDataProtection(options =>
            {
                options.ShowSensitiveData = Configuration.GetValue<bool>("Minded:ShowSensitiveData");
            });
        });
    }
}
```

### appsettings.json

```json
{
  "Minded": {
    "DataProtectionOptions": {
      "ShowSensitiveData": false
    },
    "LoggingOptions": {
      "LogLevel": "Information"
    }
  }
}
```

## Advanced Configuration

### Custom Service Lifetimes

```csharp
services.AddMinded(builder =>
{
    // Handlers are registered as Scoped by default
    // You can override this for specific handlers
    services.AddScoped<ICommandHandler<CreateUserCommand, User>, CreateUserCommandHandler>();

    // Or register as Transient
    services.AddTransient<IQueryHandler<GetUserQuery, User>, GetUserQueryHandler>();
});
```

### Conditional Decorator Registration

```csharp
services.AddMinded(builder =>
{
    // Register decorators conditionally
    if (_environment.IsDevelopment())
    {
        builder.AddCommandLoggingDecorator();
    }

    if (_featureFlags.IsEnabled("EnableRetry"))
    {
        builder.AddCommandRetryDecorator();
    }
});
```

## Best Practices

### 1. Register Decorators in Logical Order

```csharp
// Good - logical order
builder.AddCommandExceptionDecorator();   // Catch exceptions
builder.AddCommandLoggingDecorator();     // Log everything
builder.AddCommandValidationDecorator();  // Validate input

// Avoid - illogical order
builder.AddCommandValidationDecorator();  // Validation exceptions won't be logged
builder.AddCommandLoggingDecorator();
builder.AddCommandExceptionDecorator();
```

### 2. Use Assembly Filtering

```csharp
// Good - scan only your assemblies
services.AddMinded(
    assemblyFilter: assembly => assembly.Name.StartsWith("MyApp."),
    configure: builder => { });

// Avoid - scanning all assemblies (slow startup)
services.AddMinded(builder => { });
```

### 3. Configure Options from appsettings.json

```csharp
// Good - configuration from file
builder.AddDataProtection(options =>
{
    Configuration.GetSection("Minded:DataProtection").Bind(options);
});

// Avoid - hardcoded configuration
builder.AddDataProtection(options =>
{
    options.ShowSensitiveData = false;
});
```

## Troubleshooting

### Handlers Not Found

If handlers are not being discovered:

1. Ensure assemblies are being scanned:
   ```csharp
   services.AddMinded(
       assemblyFilter: assembly => assembly.Name.StartsWith("MyApp."),
       configure: builder => { });
   ```

2. Check handler implementation:
   ```csharp
   public class MyHandler : ICommandHandler<MyCommand, MyResult> { }
   ```

s## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.Configuration)
- [Main Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
