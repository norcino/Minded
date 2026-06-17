# Minded.Framework.Mediator.Abstractions

Mediator pattern abstractions for decoupled command and query dispatching.

## Features

- IMediator interface
- Command and Query dispatching contracts
- Async/await support with CancellationToken

## Installation

```bash
dotnet add package Minded.Framework.Mediator.Abstractions
```

## IMediator

`IMediator` (namespace `Minded.Framework.Mediator`) encapsulates the logic necessary to identify and invoke the specific handler needed to process a given query or command:

```csharp
public interface IMediator
{
    Task<TResult> ProcessQueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);

    Task<ICommandResponse> ProcessCommandAsync(ICommand command, CancellationToken cancellationToken = default);

    Task<ICommandResponse<TResult>> ProcessCommandAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
}
```

| Method | Purpose | Returns |
|--------|---------|---------|
| `ProcessQueryAsync<TResult>` | Resolves and executes the `IQueryHandler` for the given query | The query result of type `TResult` |
| `ProcessCommandAsync` | Resolves and executes the `ICommandHandler` for a command without a result | `ICommandResponse` with the execution outcome |
| `ProcessCommandAsync<TResult>` | Resolves and executes the `ICommandHandler` for a command returning a result | `ICommandResponse<TResult>` with the result and the execution outcome |

## Usage

Inject `IMediator` and dispatch commands and queries without referencing their handlers directly:

```csharp
using Minded.Framework.Mediator;

public class UserService
{
    private readonly IMediator _mediator;

    public UserService(IMediator mediator) => _mediator = mediator;

    public async Task<User> CreateAsync(string name, string email, CancellationToken cancellationToken = default)
    {
        var response = await _mediator.ProcessCommandAsync(new CreateUserCommand(name, email), cancellationToken);
        return response.Result;
    }

    public async Task<User> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _mediator.ProcessQueryAsync(new GetUserByIdQuery(id), cancellationToken);
}
```

The default `IMediator` implementation is provided by the [Minded.Framework.Mediator](https://www.nuget.org/packages/Minded.Framework.Mediator) package and is registered with `builder.AddMediator()`.

See the [main documentation](https://github.com/norcino/Minded) for comprehensive usage examples.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Framework.Mediator.Abstractions)
- [Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
