---
description: Rules for extending the Minded framework — adding new decorator extensions, maintaining framework internals
globs:
  - "Framework/**"
  - "Extensions/**"
  - "Tests/**"
alwaysApply: false
---

# Minded — Framework Contribution Rules

> Full details: `AI/minded-contributing.md`

## Package Conventions

- Assembly / NuGet ID: `Minded.Framework.<Name>` or `Minded.Extensions.<Name>`.
- Abstractions-only variant: `Minded.Extensions.<Name>.Abstractions`.
- Target frameworks: `netstandard2.0;net8.0;net10.0`.
- Sign with `key.snk`. Pack `README.md` as NuGet content alongside the binary.
- Folder structure inside a new extension: `Attributes/`, `Decorator/`, `Configuration/`, `ServiceCollectionExtensions.cs`.

## New Decorator Checklist

1. **Attribute** (if opt-in): sealed, `[AttributeUsage(AttributeTargets.Class, Inherited = false)]`, XML-documented.
2. **Decorator classes**: implement both the void-result variant (`CommandHandlerDecoratorBase<TCommand>`) and the generic-result variant (`CommandHandlerDecoratorBase<TCommand, TResult>`). Same for query decorators.
3. **Pass-through when inactive**: always check for the attribute and delegate to `InnerHandler` unchanged when absent.
4. **Registration extension**: `Add<Name>Decorator(this MindedBuilder builder)` using open-generic `ServiceDescriptor` with `ServiceLifetime.Transient`.
5. **Tests**: test "attribute absent → pass-through" and "attribute present → behaviour applied".
6. **README.md**: document purpose, installation, attribute usage, and DI registration.

## Decorator Skeleton

```csharp
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

        // pre-processing...
        var response = await InnerHandler.HandleAsync(command, cancellationToken);
        // post-processing...
        return response;
    }
}
```

## Registration Skeleton

```csharp
public static MindedBuilder AddMyBehaviourDecorator(this MindedBuilder builder)
{
    builder.Register(sc =>
    {
        sc.Add(new ServiceDescriptor(typeof(ICommandHandler<>),
            typeof(MyBehaviourCommandHandlerDecorator<>), ServiceLifetime.Transient));
        sc.Add(new ServiceDescriptor(typeof(ICommandHandler<,>),
            typeof(MyBehaviourCommandHandlerDecorator<,>), ServiceLifetime.Transient));
    });
    return builder;
}
```

## Coding Conventions

- XML docs required on all public types and members.
- `ArgumentNullException.ThrowIfNull(param)` for null guards at method entry.
- `InvalidOperationException` for invalid runtime state.
- All I/O via `async/await`; never `.Result` or `.Wait()`.
- MSTest + Moq + FluentAssertions for tests.
- `<VersionPrefix>` in `.csproj`; central dependency versions in `Directory.Packages.props`.
