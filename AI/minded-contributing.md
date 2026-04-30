# Minded Framework — Contribution Guide

Guidelines for **extending and maintaining** the Minded framework itself: packages in `Framework/` and `Extensions/`.

---

## Framework Architecture

```
IMediator
  └── Resolves ICommandHandler<TCommand, TResult> / IQueryHandler<TQuery, TResult>
        └── Decorator chain built by DI via MindedBuilder
              First registered = innermost (runs last, closest to handler)
              Last registered  = outermost (runs first, earliest interception)
```

**Mediator resolution**: `Mediator` resolves handlers by command/query type from `IServiceProvider`, caches handler types and compiled delegates keyed by `(commandType, handlerInterface)`. Falls back to `dynamic` dispatch for mock/proxy scenarios (integration tests).

**Decorator chain**: built through DI's open-generic registration. Each decorator takes `ICommandHandler<TCommand, TResult>` (or the query equivalent) as a constructor dependency, forming a chain. `AddCommandHandlers()` / `AddQueryHandlers()` register concrete handler types and are always innermost regardless of call order.

---

## Package Naming and Structure

| Convention | Rule |
|------------|------|
| Assembly / NuGet ID | `Minded.Framework.<Name>` or `Minded.Extensions.<Name>` |
| Abstractions package | `Minded.Extensions.<Name>.Abstractions` — interfaces only, no DI |
| Namespace root | Mirrors assembly name exactly (e.g. `Minded.Extensions.MyBehaviour`) |
| Target frameworks | `netstandard2.0;net8.0;net10.0` |
| Signing | `<SignAssembly>true</SignAssembly>` with `key.snk` at the repo root |
| NuGet metadata | `PackageId`, `Description`, `PackageTags`, `<None Include="README.md">` packed as content |
| Symbol packages | `<IncludeSymbols>true</IncludeSymbols>` with `snupkg` format |

---

## Adding a New Decorator Extension

### Step 1 — Create the project

Create `Extensions/Minded.Extensions.<Name>/Minded.Extensions.<Name>.csproj` targeting `netstandard2.0;net8.0;net10.0`. Copy the NuGet metadata block from an existing extension project (e.g. `Minded.Extensions.Logging`). Add a `README.md` to be packed with the NuGet.

### Step 2 — Create an opt-in attribute (if attribute-driven)

```csharp
// Attributes/MyBehaviourAttribute.cs
namespace Minded.Extensions.MyBehaviour.Attributes
{
    /// <summary>Enables MyBehaviour processing on a command or query when the corresponding decorator is registered.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class MyBehaviourAttribute : Attribute
    {
        /// <summary>Gets the primary configuration value.</summary>
        public int SomeOption { get; }

        /// <summary>Initializes the attribute with optional configuration.</summary>
        public MyBehaviourAttribute(int someOption = 0) => SomeOption = someOption;
    }
}
```

### Step 3 — Implement the command decorator

Extend `CommandHandlerDecoratorBase<TCommand>` (void result) and `CommandHandlerDecoratorBase<TCommand, TResult>` (with result):

```csharp
// Decorator/MyBehaviourCommandHandlerDecorator.cs
namespace Minded.Extensions.MyBehaviour.Decorator
{
    /// <summary>
    /// Applies MyBehaviour around command execution when <see cref="MyBehaviourAttribute"/> is present on the command.
    /// </summary>
    public class MyBehaviourCommandHandlerDecorator<TCommand>
        : CommandHandlerDecoratorBase<TCommand>
        where TCommand : ICommand
    {
        public MyBehaviourCommandHandlerDecorator(ICommandHandler<TCommand> innerHandler)
            : base(innerHandler) { }

        /// <inheritdoc />
        public override async Task<ICommandResponse> HandleAsync(
            TCommand command, CancellationToken cancellationToken = default)
        {
            if (!command.GetType().IsDefined(typeof(MyBehaviourAttribute), false))
                return await InnerHandler.HandleAsync(command, cancellationToken);

            var attr = command.GetType().GetCustomAttribute<MyBehaviourAttribute>()!;
            // pre-processing using attr.SomeOption ...
            var response = await InnerHandler.HandleAsync(command, cancellationToken);
            // post-processing ...
            return response;
        }
    }

    /// <summary>Result-returning variant.</summary>
    public class MyBehaviourCommandHandlerDecorator<TCommand, TResult>
        : CommandHandlerDecoratorBase<TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        public MyBehaviourCommandHandlerDecorator(ICommandHandler<TCommand, TResult> innerHandler)
            : base(innerHandler) { }

        /// <inheritdoc />
        public override async Task<ICommandResponse<TResult>> HandleAsync(
            TCommand command, CancellationToken cancellationToken = default)
        {
            if (!command.GetType().IsDefined(typeof(MyBehaviourAttribute), false))
                return await InnerHandler.HandleAsync(command, cancellationToken);

            return await InnerHandler.HandleAsync(command, cancellationToken);
        }
    }
}
```

Similarly, create `QueryHandlerDecoratorBase<TQuery, TResult>` variants when query support is needed.

### Step 4 — Register via MindedBuilder

```csharp
// ServiceCollectionExtensions.cs
namespace Minded.Extensions.MyBehaviour
{
    /// <summary>Registration extensions for the MyBehaviour decorator.</summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>Adds the MyBehaviour decorator to the command pipeline.</summary>
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
    }
}
```

### Step 5 — Write tests

Create `Tests/Minded.Extensions.MyBehaviour.Tests/`:

```csharp
[TestClass]
public class MyBehaviourCommandHandlerDecoratorTests
{
    [TestMethod]
    public async Task Decorator_passes_through_when_attribute_is_absent()
    {
        var innerMock = new Mock<ICommandHandler<PlainCommand>>();
        innerMock.Setup(h => h.HandleAsync(It.IsAny<PlainCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new CommandResponse(true));

        var sut = new MyBehaviourCommandHandlerDecorator<PlainCommand>(innerMock.Object);
        await sut.HandleAsync(new PlainCommand());

        innerMock.Verify(h => h.HandleAsync(It.IsAny<PlainCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Decorator_applies_behaviour_when_attribute_is_present()
    {
        // Arrange, Act, Assert ...
    }
}

// Helper — a command WITHOUT the attribute
public class PlainCommand : ICommand { }

// Helper — a command WITH the attribute
[MyBehaviour]
public class DecoratedCommand : ICommand { }
```

### Step 6 — Write the extension README.md

Document: purpose, NuGet installation, attribute usage, DI registration, and supported command/query types.

---

## Decorator Registration Order Rule

> **First registered = innermost (runs last, right before the handler). Last registered = outermost (runs first).**

Recommended order (first to last, innermost to outermost):

```csharp
builder.AddCommandValidationDecorator()   // innermost: validates right before handler
       .AddCommandRetryDecorator()        // retries handler + validation on exception
       .AddCommandLoggingDecorator()      // logs each attempt
       .AddCommandExceptionDecorator()    // outermost: catches all unhandled exceptions
       .AddCommandHandlers();             // actual handlers — always innermost regardless of position
```

---

## Coding Conventions

| Rule | Detail |
|------|--------|
| XML documentation | Required on **all** public types and members (`/// <summary>`) |
| Null guards | `ArgumentNullException.ThrowIfNull(param)` or `throw new ArgumentNullException(nameof(param))` |
| Invalid state | `throw new InvalidOperationException("reason")` |
| Async | All I/O is `async/await`; never `.Result` or `.Wait()` |
| Guard placement | Top of methods; no deep defensive nesting |
| Test framework | MSTest (`[TestClass]`, `[TestMethod]`, `[TestInitialize]`) |
| Test assertions | FluentAssertions (`result.Should().BeTrue()`) |
| Test mocking | Moq (`new Mock<T>()`, `.Setup()`, `.Verify()`) |
| Namespaces | Mirror assembly name exactly |
| Folder structure | `Attributes/`, `Decorator/`, `Configuration/`, `ServiceCollectionExtensions.cs` |

---

## Versioning

- Set per-project in `.csproj`: `<VersionPrefix>`, `<AssemblyVersion>`, `<FileVersion>`.
- Central dependency versions managed in `Directory.Packages.props`.
- Follow semantic versioning: patch → bug fix, minor → new feature (non-breaking), major → breaking API change.
- Do not edit `Directory.Build.props` without understanding the cross-project impact.
