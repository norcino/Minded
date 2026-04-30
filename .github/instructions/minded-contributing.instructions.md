---
applyTo: "Framework/**/*.cs,Extensions/**/*.cs,Tests/**/*.cs"
---

# Minded Framework — Contribution Rules

> Full details: `AI/minded-contributing.md`. These rules apply when editing framework and extension source code.

---

## Package Structure

- Assembly / NuGet ID: `Minded.Framework.<Name>` or `Minded.Extensions.<Name>`.
- Abstractions-only packages: `Minded.Extensions.<Name>.Abstractions` (interfaces, no DI dependencies).
- Target frameworks: `netstandard2.0;net8.0;net10.0`.
- Sign with `key.snk`. Pack `README.md` as NuGet content.
- Folder layout: `Attributes/`, `Decorator/`, `Configuration/`, `ServiceCollectionExtensions.cs`.

---

## Implementing a New Decorator

Every decorator **must** provide both the void-result (`ICommand`) and generic-result (`ICommand<TResult>`) variants:

```csharp
// Void-result variant
public class MyBehaviourCommandHandlerDecorator<TCommand>
    : CommandHandlerDecoratorBase<TCommand>
    where TCommand : ICommand
{
    public MyBehaviourCommandHandlerDecorator(ICommandHandler<TCommand> innerHandler)
        : base(innerHandler) { }

    public override async Task<ICommandResponse> HandleAsync(
        TCommand command, CancellationToken cancellationToken = default)
    {
        if (!command.GetType().IsDefined(typeof(MyBehaviourAttribute), false))
            return await InnerHandler.HandleAsync(command, cancellationToken);

        // pre / post processing around InnerHandler.HandleAsync(...)
        return await InnerHandler.HandleAsync(command, cancellationToken);
    }
}

// Generic-result variant
public class MyBehaviourCommandHandlerDecorator<TCommand, TResult>
    : CommandHandlerDecoratorBase<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public MyBehaviourCommandHandlerDecorator(ICommandHandler<TCommand, TResult> innerHandler)
        : base(innerHandler) { }

    public override async Task<ICommandResponse<TResult>> HandleAsync(
        TCommand command, CancellationToken cancellationToken = default)
    {
        if (!command.GetType().IsDefined(typeof(MyBehaviourAttribute), false))
            return await InnerHandler.HandleAsync(command, cancellationToken);

        return await InnerHandler.HandleAsync(command, cancellationToken);
    }
}
```

---

## Registering via MindedBuilder

```csharp
public static MindedBuilder AddMyBehaviourDecorator(this MindedBuilder builder)
{
    builder.Register(sc =>
    {
        sc.Add(new ServiceDescriptor(
            typeof(ICommandHandler<>),
            typeof(MyBehaviourCommandHandlerDecorator<>),
            ServiceLifetime.Transient));
        sc.Add(new ServiceDescriptor(
            typeof(ICommandHandler<,>),
            typeof(MyBehaviourCommandHandlerDecorator<,>),
            ServiceLifetime.Transient));
    });
    return builder;
}
```

---

## Decorator Registration Order

**First registered = innermost (runs last, right before the handler). Last registered = outermost (runs first).**

Recommended order for consuming apps (innermost → outermost):

```csharp
builder.AddCommandValidationDecorator()   // innermost
       .AddCommandRetryDecorator()
       .AddCommandLoggingDecorator()
       .AddCommandExceptionDecorator()    // outermost
       .AddCommandHandlers();
```

---

## Coding Conventions

- **XML docs**: required on all public types and members.
- **Null guards**: `ArgumentNullException.ThrowIfNull(param)` at method entry.
- **Invalid state**: `throw new InvalidOperationException("reason")`.
- **Async**: all I/O via `async/await`; never `.Result` or `.Wait()`.
- **Tests**: MSTest (`[TestClass]`, `[TestMethod]`), Moq, FluentAssertions. Test both "attribute absent" and "attribute present" paths.
- **Versioning**: `<VersionPrefix>` in `.csproj`; central deps in `Directory.Packages.props`.
