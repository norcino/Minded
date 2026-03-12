# Minded.Framework.Decorator

Base classes and utilities for implementing the Decorator pattern in CQRS pipelines. This package provides the foundation for creating custom decorators that add cross-cutting concerns to command and query handlers.

## Features

- **CommandHandlerDecoratorBase<TCommand>** - Base class for command decorators
- **QueryHandlerDecoratorBase<TQuery, TResult>** - Base class for query decorators
- **IDecoratingAttributeValidator** - Interface for validating decorator attribute usage
- Pipeline composition support
- Cross-cutting concern infrastructure

## Installation

```bash
dotnet add package Minded.Framework.Decorator
```

## Core Classes

### CommandHandlerDecoratorBase<TCommand>

Base class for creating command decorators. Provides access to the next handler in the decorator chain.

**Constructor:**
```csharp
protected CommandHandlerDecoratorBase(ICommandHandler<TCommand> commandHandler)
```

**Properties:**
- `DecoratedCommmandHandler` - The next command handler in the chain (note: typo is intentional for backward compatibility)

**Example:**
```csharp
using Minded.Framework.Decorator;
using Minded.Framework.CQRS.Abstractions.Command;

public class MyCommandDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private readonly ILogger _logger;

    public MyCommandDecorator(
        ICommandHandler<TCommand> commandHandler,
        ILogger<MyCommandDecorator<TCommand>> logger) : base(commandHandler)
    {
        _logger = logger;
    }

    public async Task<ICommandResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing command {CommandType}", typeof(TCommand).Name);

        var response = await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);

        _logger.LogInformation("Command {CommandType} completed", typeof(TCommand).Name);

        return response;
    }
}
```

### QueryHandlerDecoratorBase<TQuery, TResult>

Base class for creating query decorators. Provides access to the next handler in the decorator chain.

**Constructor:**
```csharp
protected QueryHandlerDecoratorBase(IQueryHandler<TQuery, TResult> queryHandler)
```

**Properties:**
- `InnerQueryHandler` - The next query handler in the chain

**Example:**
```csharp
using Minded.Framework.Decorator;
using Minded.Framework.CQRS.Abstractions.Query;

public class MyQueryDecorator<TQuery, TResult> : QueryHandlerDecoratorBase<TQuery, TResult>, IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    private readonly ILogger _logger;

    public MyQueryDecorator(
        IQueryHandler<TQuery, TResult> queryHandler,
        ILogger<MyQueryDecorator<TQuery, TResult>> logger) : base(queryHandler)
    {
        _logger = logger;
    }

    public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing query {QueryType}", typeof(TQuery).Name);

        var result = await InnerQueryHandler.HandleAsync(query, cancellationToken);

        _logger.LogInformation("Query {QueryType} completed", typeof(TQuery).Name);

        return result;
    }
}
```

### IDecoratingAttributeValidator

Interface for validating that commands/queries with specific attributes implement required interfaces.

**Example:**
```csharp
using Minded.Framework.Decorator;

public class MyCacheAttributeValidator : IDecoratingAttributeValidator
{
    public void Validate(Func<AssemblyName, bool> assemblyFilter)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => assemblyFilter(a.GetName()))
            .ToList();

        foreach (var assembly in assemblies)
        {
            var typesWithAttribute = assembly.GetTypes()
                .Where(t => t.GetCustomAttributes(typeof(MyCacheAttribute), true).Any());

            foreach (var type in typesWithAttribute)
            {
                if (!typeof(IGenerateCacheKey).IsAssignableFrom(type))
                {
                    throw new InvalidOperationException(
                        $"Type {type.Name} has [MyCache] attribute but doesn't implement IGenerateCacheKey");
                }
            }
        }
    }
}
```

**Note**: Attribute validators are automatically discovered and executed during application startup. Do not register them manually in the DI container.

## Creating Custom Decorators

### Step 1: Create the Decorator Class

```csharp
public class TimingDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private readonly ILogger<TimingDecorator<TCommand>> _logger;

    public TimingDecorator(
        ICommandHandler<TCommand> commandHandler,
        ILogger<TimingDecorator<TCommand>> logger) : base(commandHandler)
    {
        _logger = logger;
    }

    public async Task<ICommandResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation("Command {CommandType} took {ElapsedMs}ms",
            typeof(TCommand).Name, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
```

### Step 2: Create Extension Method for Registration

```csharp
using Minded.Extensions.Configuration;

public static class TimingDecoratorExtensions
{
    public static MindedBuilder AddCommandTimingDecorator(this MindedBuilder builder)
    {
        builder.QueueCommandDecoratorRegistrationAction((b, i) =>
            b.DecorateHandlerDescriptors(i, typeof(TimingDecorator<>)));

        return builder;
    }
}
```

### Step 3: Register the Decorator

```csharp
services.AddMinded(builder =>
{
    builder.AddCommandValidationDecorator()
           .AddCommandTimingDecorator()      // Your custom decorator
           .AddCommandExceptionDecorator()
           .AddCommandHandlers();
});
```

## Decorator Execution Order

Decorators are registered from **innermost to outermost**, but execute in **reverse order**:

```csharp
builder.AddCommandValidationDecorator()    // 1. Registered first (innermost)
       .AddCommandLoggingDecorator()        // 2. Registered second
       .AddCommandExceptionDecorator()      // 3. Registered last (outermost)
       .AddCommandHandlers();

// Execution order:
// Exception → Logging → Validation → Handler → Validation → Logging → Exception
```

## Best Practices

1. **First Parameter Must Be Decorated Handler** - The first constructor parameter must be the decorated handler
2. **Pass CancellationToken** - Always pass the cancellation token through the chain
3. **Avoid Heavy Operations** - Decorators run on every request; keep them lightweight
4. **Use Dependency Injection** - Inject services via constructor (after the decorated handler)
5. **Let Exceptions Bubble** - Don't catch exceptions unless you have a specific reason
6. **Test in Isolation** - Write unit tests for decorators independently of handlers

## Usage

See the [main documentation](https://github.com/norcino/Minded#for-engineers-extending-minded) for comprehensive usage examples and advanced scenarios.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Framework.Decorator)
- [Main Documentation](https://github.com/norcino/Minded#readme)
- [Extending Minded Guide](https://github.com/norcino/Minded#for-engineers-extending-minded)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
