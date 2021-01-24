[![Build status](https://dev.azure.com/norcino/Minded/_apis/build/status/GitHub%20Minded)](https://dev.azure.com/norcino/Minded/_build/latest?definitionId=1)


# Minded
Clean code and architecture made simple with Mediator, Command Query and Decorator patterns.


## Introduction
The Minded framework take it's name in a stretchy way from "**M**ed**I**ator comma**ND** qu**E**ry **D**ecorator". As you can see those are the top patterns used in the framework to help create simple yet effective architectures, where the structure of the framework will force you to think about Encapsulation, Reusability, Maintainability, Extension, Low Coupling and more.

Correct, this is not the silver bullet framework and will not magically turn people in experienced professionals, but it will help.


### Requirements
This framework requires [.net Core](https://dotnet.microsoft.com/learn/dotnet/what-is-dotnet) 2.0 upward, it can be used in brand new applications or retrofitted in existing applications where it can be used to break down monolitic components.


### How does it work


**The entry point (Mediator)**

The implementation of [IMediator](https://github.com/norcino/Minded/blob/master/Mediator/IMediator.cs) interface ([Mediator Design Pattern](https://en.wikipedia.org/wiki/Mediator_pattern)), is injected everywhere there is a dependency on business logic or where it is needed to retrieve data.

The Mediator allows to hide and decouple from the mechanism used to execute commands and queries, so called Handlers.


**The encapsulation of the business logic (Command Query)**

The consumer component is responsible for the creation of an instance of [ICommand](https://github.com/norcino/Minded/blob/master/CommandQuery/Command/ICommand.cs) or [IQuery](https://github.com/norcino/Minded/blob/master/CommandQuery/Query/IQuery.cs) (from now on I will refer to Command as Command or Query) ([Command Design Pattern](https://en.wikipedia.org/wiki/Command_pattern) and [Command Query](https://en.wikipedia.org/wiki/Command%E2%80%93query_separation)). Using the mediator, the Command is passed for the execution, and following the [Dependency Injection Configuration](https://github.com/norcino/Minded/tree/master/Configuration), one or more [ICommandHandler](https://github.com/norcino/Minded/blob/master/CommandQuery/Command/ICommandHandler.cs) or [IQueryHandler](https://github.com/norcino/Minded/blob/master/CommandQuery/Query/IQueryHandler.cs), will be instantiated to process the command or the query.

Note that a single command can have multiple Handlers responsible to process it.


**The decoration (Decorators)**

When an Handler is instantiated, by default it is decorated with:
* Validation decorator
* Transaction handling decorator
* Exception handling decorator
* Logging command decorator

Each decorator represent a layer around the handler, like an onion, each layer has and must have a single responsibility and the order used to register them in the dependency injection, is important to drive the order of execution.


**Command processing flow**

So when **Mediator.ProcessCommandAsync** is invoked to process a **Command**, the dependency injection will retrieve and instantiate all the handlers (normally one) designed to handle the Command execution. Once the **Handler** is instantiated, the dependency injection framework will instantiate one by one all the decorators registered for the Command Handling, starting from the last **Decorator** that will be executed just before the actual Handler.

The Handle method of the last registered Decorator will be invoked passing the Command, based on the Decorator responsibility, something might happen straight away or only after the next Handler (which could either be another Decorator or the fina Handler) has been executed.

This works very much like the .net Core Middlewares, where a request (ICommand) comes in, follow the pipeline and then returns back as response ([CommandResponse](https://github.com/norcino/Minded/blob/master/CommandQuery/Command/CommandResponse.cs)).


### Limitations
This framework targets .net Standard 2.0, but this doesn't make it directly compatible with .net Freamework applications.

Interfaces like [IServiceCollection](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection?view=dotnet-plat-ext-3.1) and [ILogger](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger?view=dotnet-plat-ext-3.1), are used to control configuration and logging, and even tough they can be integrated with .net Framework application, I don't think the effort would be worth.


## Getting started
First of all install the [Minded Nuget Package](https://www.nuget.org/packages/Minded/), uwinf the **Visual Studio Package Manager**, or the **Packat Manager Console** using the command _Install-Package Elmah -ProjectName {Target_Project_Name}_.


### Service configuration


## Decorators
Decorators are provided with the framework for both Query and Command handling.

### Error decorator
[ExceptionCommandHandlerDecorator](https://github.com/norcino/Minded/blob/master/Decorator/Exception/ExceptionCommandHandlerDecorator.cs) and [ExceptionQueryHandlerDecorator](https://github.com/norcino/Minded/blob/master/Decorator/Exception/ExceptionQueryHandlerDecorator.cs) are designed to handle Exceptions in a single place. The decorators depend on the logging system used to log the details of the exception.
This is a basic implementation, a more elaborated version can be used instead leveraging custom attributes.

### Logging decorator
[LoggingCommandHandlerDecorator](https://github.com/norcino/Minded/blob/master/Decorator/Logging/LoggingCommandHandlerDecorator.cs) and [LoggingQueryHandlerDecorator](https://github.com/norcino/Minded/blob/master/Decorator/Logging/LoggingQueryHandlerDecorator.cs) are responsible to log the execution of each command and query, including information like the time needed for the execution.

### Validation decorator
Probably the best example on how decorators can be used with their great potential.
[ValidationCommandHandlerDecorator](https://github.com/norcino/Minded/blob/master/Decorator/Validation/ValidationCommandHandlerDecorator.cs) allows you to add validation for each command, to do so the command class has to use the [ValidateCommandAttribute](https://github.com/norcino/Minded/blob/master/Decorator/Validation/ValidateCommandAttribute.cs).

When the decorator is invoked, it takes the command as parameter, this is inspected and if the attribute is found, the injected [ICommandValidator](https://github.com/norcino/Minded/blob/master/Decorator/Validation/ICommandValidator.cs) implementation, is used to validate the command. If the command is valid, the next handler will be invoked, otherwhise and instance of [CommandResponse](https://github.com/norcino/Minded/blob/master/CommandQuery/Command/CommandResponse.cs) will be returned straight away.

_NOTE: The Validation decorator can be a terminal handler if the validation does not succeed, interrupting the execution pipeline and preventing the next handlers to be called._

### Transaction decorator
[...]

## Recommendations
[...]

## A bit of history and thanks
A first draft of this architecture was originally designed by me and my team when I was working as Tech Lead in a company where we were trying o rewrite a monolitic legacy application into a more modern and cleaner architecture where the first goal was to create simplified maitainable code, with the possibility to scale.

The original implementation was made in .net Framework and been used for years proving itself to be a very effective solution to deliver good code, easy to maintain and test.
Encapsulating the business logic in commands, has also facilitated the migration from syncronous operations to asynchronous, wrapping Commands and CommandHandlers in a Distributed Messageing System commands and handlers, allowing us the break down in smaller services the logic which was more I/O intensive to, and to support the execution of batches of commands.

In the last few years I wrote from scratch the code in this repository, with the intent to apply the best practices I was using and improve them with new ideas, implementing the whole in .net Core, nowdays the future of .net.
The last step has been the migration to this public repository to share it with the world, and make it a NuGet package, so that it will be easier for me and everyone else to use it.