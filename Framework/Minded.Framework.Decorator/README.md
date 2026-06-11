# Minded.Framework.Decorator

Base classes and utilities for implementing the Decorator pattern in CQRS pipelines. This package provides the foundation for creating custom decorators that add cross-cutting concerns to command and query handlers.

## Features

- **CommandHandlerDecoratorBase<TCommand>** - Base class for decorators of commands without a result
- **CommandHandlerDecoratorBase<TCommand, TResult>** - Base class for decorators of commands returning a result
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

Base class for creating decorators of commands without a result. Provides access to the next handler in the decorator chain.

**Constructor:**
```csharp
protected CommandHandlerDecoratorBase(ICommandHandler<TCommand> commandHandler)
```

**Members:**
- `InnerCommandHandler` - Public property exposing the next command handler in the chain. **This correctly-spelled alias is the preferred accessor.**
- `DecoratedCommmandHandler` - Protected field holding the same handler (note: the typo is intentional and kept for backward compatibility; `InnerCommandHandler` simply returns it)

**Example:**
```csharp
using Minded.Framework.Decorator;
using Minded.Framework.CQRS.Command;

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

        var response = await InnerCommandHandler.HandleAsync(command, cancellationToken);

        _logger.LogInformation("Command {CommandType} completed", typeof(TCommand).Name);

        return response;
    }
}
```

### CommandHandlerDecoratorBase<TCommand, TResult>

Base class for creating decorators of commands that return a result (`ICommand<TResult>`). It exposes the same member pair as the void variant, typed for result-returning handlers.

**Constructor:**
```csharp
protected CommandHandlerDecoratorBase(ICommandHandler<TCommand, TResult> commandHandler)
```

**Members:**
- `InnerCommandHandler` - Public `ICommandHandler<TCommand, TResult>` property exposing the next handler in the chain (preferred accessor)
- `DecoratedCommmandHandler` - Protected field holding the same handler (intentional historical typo)

**Example:**
```csharp
using Minded.Framework.Decorator;
using Minded.Framework.CQRS.Command;

public class MyCommandDecorator<TCommand, TResult> : CommandHandlerDecoratorBase<TCommand, TResult>, ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public MyCommandDecorator(ICommandHandler<TCommand, TResult> commandHandler) : base(commandHandler)
    {
    }

    public async Task<ICommandResponse<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        // pre-processing ...
        var response = await InnerCommandHandler.HandleAsync(command, cancellationToken);
        // post-processing ...
        return response;
    }
}
```

### QueryHandlerDecoratorBase<TQuery, TResult>

Base class for creating query decorators. Provides access to the next handler in the decorator chain.

**Constructor:**
```csharp
public QueryHandlerDecoratorBase(IQueryHandler<TQuery, TResult> queryHandler)
```

**Members:**
- `InnerQueryHandler` - Public property exposing the next query handler in the chain (preferred accessor)
- `DecoratedQueryHandler` - Protected field holding the same handler

**Example:**
```csharp
using Minded.Framework.Decorator;
using Minded.Framework.CQRS.Query;

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

        var response = await InnerCommandHandler.HandleAsync(command, cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation("Command {CommandType} took {ElapsedMs}ms",
            typeof(TCommand).Name, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
```

### Step 2: Create Extension Method for Registration

The following extension method is example code that you write in your own project — it is not part of this package. It uses the `MindedBuilder` from `Minded.Extensions.Configuration` to queue the decorator registration:

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
services.AddMinded(
    configuration,
    assembly => assembly.FullName.StartsWith("YourApp"),
    builder =>
    {
        builder.AddCommandValidationDecorator()
               .AddCommandTimingDecorator()      // Your custom decorator
               .AddCommandExceptionDecorator()
               .AddCommandHandlers();
    });
```

## Decorator Execution Order

The registration order maps directly onto the position in the chain:

- **First registered = innermost** — closest to the handler, it runs last, right before the handler.
- **Last registered = outermost** — it runs first, intercepting the call earliest.

```csharp
builder.AddCommandValidationDecorator()    // 1. Registered first  = innermost (runs last, right before the handler)
       .AddCommandLoggingDecorator()        // 2. Registered second = in between
       .AddCommandExceptionDecorator()      // 3. Registered last   = outermost (runs first)
       .AddCommandHandlers();               // Handlers are always the innermost element

// Execution order:
// Exception → Logging → Validation → Handler
```

With Validation registered first and Exception registered last, a command first enters the Exception decorator, then Logging, then Validation, and finally reaches the handler. The response then flows back outward through the same decorators in reverse (Validation, Logging, Exception). This is why the exception decorator must be registered last: being outermost, it wraps everything and can catch errors thrown anywhere in the chain.

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
