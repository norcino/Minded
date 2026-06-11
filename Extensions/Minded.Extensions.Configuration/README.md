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
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration) => Configuration = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMinded(Configuration, mindedBuilderConfiguration: builder =>
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
    mindedBuilderConfiguration: builder =>
    {
        builder.AddCommandValidationDecorator();
        builder.AddCommandLoggingDecorator();
    });
```

### Configuration with Options

```csharp
services.AddMinded(configuration, mindedBuilderConfiguration: builder =>
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
builder.AddCommandValidationDecorator();      // Validation (Minded.Extensions.Validation)
builder.AddCommandLoggingDecorator();         // Logging (Minded.Extensions.Logging)
builder.AddCommandExceptionDecorator();       // Exception handling (Minded.Extensions.Exception)
builder.AddCommandRetryDecorator();           // Retry logic (Minded.Extensions.Retry)
builder.AddCommandTransactionDecorator();     // Transactions (Minded.Extensions.Transaction)
```

> There is no command caching decorator — caching is supported for queries only.

### Query Decorators

```csharp
builder.AddQueryValidationDecorator();        // Validation (Minded.Extensions.Validation)
builder.AddQueryLoggingDecorator();           // Logging (Minded.Extensions.Logging)
builder.AddQueryExceptionDecorator();         // Exception handling (Minded.Extensions.Exception)
builder.AddQueryRetryDecorator();             // Retry logic (Minded.Extensions.Retry)
builder.AddQueryMemoryCacheDecorator();       // In-memory caching (Minded.Extensions.Caching.Memory)
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
services.AddMinded(configuration);
```

### Filtered Scanning

```csharp
// Scan only assemblies matching the filter
services.AddMinded(
    configuration,
    assemblyFilter: assembly => assembly.Name.StartsWith("MyApp."));
```

### Explicit Assembly Registration

```csharp
// Register handlers from specific assemblies using a per-call filter
services.AddMinded(configuration, mindedBuilderConfiguration: builder =>
{
    builder.AddCommandHandlers(assemblyFilter: assembly => assembly.Name.StartsWith("MyApp.Application"));
    builder.AddQueryHandlers(assemblyFilter: assembly => assembly.Name.StartsWith("MyApp.Application"));
});
```

## Decorator Order

Registration order determines nesting: **first registered = innermost** (runs last, right before the handler); **last registered = outermost** (runs first). The exception decorator must be registered last so it is outermost and catches errors from every other decorator. The recommended order is:

```csharp
services.AddMinded(configuration, mindedBuilderConfiguration: builder =>
{
    // 1. Validation (innermost - validates right before the handler)
    builder.AddCommandValidationDecorator();

    // 2. Retry (retries handler + validation on transient failures)
    builder.AddCommandRetryDecorator();

    // 3. Logging (logs each attempt)
    builder.AddCommandLoggingDecorator();

    // 4. Exception handling (outermost - catches all unhandled exceptions)
    builder.AddCommandExceptionDecorator();

    // Actual handlers - always innermost regardless of registration position
    builder.AddCommandHandlers();
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
services.AddMinded(configuration, mindedBuilderConfiguration: builder =>
{
    // Handlers are registered as Transient by default.
    // Override the lifetime for all handlers via the lifeTime parameter:
    builder.AddCommandHandlers(lifeTime: ServiceLifetime.Scoped);
    builder.AddQueryHandlers(lifeTime: ServiceLifetime.Scoped);
});
```

### Conditional Decorator Registration

```csharp
services.AddMinded(configuration, mindedBuilderConfiguration: builder =>
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
// Good - exception decorator registered last (outermost), so it catches
// errors from validation, logging and the handler
builder.AddCommandValidationDecorator();  // Innermost - validate right before the handler
builder.AddCommandLoggingDecorator();     // Log everything
builder.AddCommandExceptionDecorator();   // Outermost - catch all exceptions

// Avoid - exception decorator registered first becomes innermost and
// misses errors thrown by the other decorators
builder.AddCommandExceptionDecorator();
builder.AddCommandLoggingDecorator();
builder.AddCommandValidationDecorator();
```

### 2. Use Assembly Filtering

```csharp
// Good - scan only your assemblies
services.AddMinded(
    configuration,
    assemblyFilter: assembly => assembly.Name.StartsWith("MyApp."));

// Avoid - scanning all assemblies (slow startup)
services.AddMinded(configuration);
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

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.Configuration)
- [Main Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
