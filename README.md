[![Build status](https://dev.azure.com/norcino/Minded/_apis/build/status/GitHub%20Minded)](https://dev.azure.com/norcino/Minded/_build/latest?definitionId=1)

# Minded
Clean code and architecture made simple with Mediator, Command Query and Decorator patterns.


## Introduction
The Minded framework take it's name in a stretchy way from "**M**ed**I**ator comma**ND** qu**E**ry **D**ecorator". As you can see those are the top patterns used in the framework to help create simple yet effective architectures, where the structure of the framework will force you to think about Encapsulation, Reusability, Maintainability, Extension, Low Coupling and more.

Correct, this is not the silver bullet framework and will not magically turn people in experienced professionals, but it will help.

### Mediator
The Mediator is a design pattern that promotes loose coupling and enhances code maintainability. It reduces the direct communication between objects, so changes in one object don't have a ripple effect on others. This is achieved by introducing a mediator object that controls how objects interact with each other. The primary benefits include improved modularity, easier reuse of individual components, and simpler communication protocols. In complex systems where a multitude of classes interact, employing the Mediator pattern can significantly simplify the system architecture, making it easier to understand, modify, and extend. It also fosters the Single Responsibility Principle, where each class is responsible for a single part of the application's functionality, contributing to the overall robustness and resilience of the system.

### Decorator
The Decorator pattern is a design pattern in object-oriented programming that allows behavior to be added to an individual object, either statically or dynamically, without affecting the behavior of other objects from the same class. This is particularly useful when you need to add responsibilities to objects without subclassing. It enhances flexibility and is in line with the Single Responsibility and Open/Closed principles of SOLID design principles, as it allows functionality to be divided between classes with unique areas of concern and enables the behavior of an object to be extended without modifying its source code. Decorators provide a flexible alternative to subclassing for extending functionality, allowing for a large number of distinct behaviors. Additionally, Decorator offers a more scalable solution than static inheritance, as you can add or remove responsibilities from an object at runtime by wrapping it in different decorator classes.

### Requirements
This framework requires [.net Core](https://dotnet.microsoft.com/learn/dotnet/what-is-dotnet) 2.0 upward, it can be used in brand new applications or retrofitted in existing applications where it can be used to break down monolitic components.

## Features
The framework offers support for clean code design, efficiency, and maintainability. It provides a set of features that can be used to create a robust and scalable architecture.

### Mediator
The Mediator opbject allows to decouple the business logic from the implementation, by using the Mediator pattern, the business logic is encapsulated in a single object, which is responsible for coordinating the interaction between objects. This promotes loose coupling and enhances code maintainability, making it easier to understand, modify, and extend the system.
It works based on the Command Query pattern, where every action is represented as a Command or Query object, and the Mediator is responsible for executing these objects.
It works leveraging the automatic dependency injection configuration, where the Command and Query handlers are automatically registered in the DI container.
Any additional class necessary to implement specific features like Validation, which is available out of the box, is also registered in the DI container automatically.

### RestMediator
This extension available with the package [Minded.Extensions.WebApi](https://www.nuget.org/packages/Minded.Extensions.WebApi/), allows to use the Mediator pattern with RESTful APIs, to streamline the API request handling.
The class _DefaultRestRulesProcesor_ at each incoming request processes every rule speficied in the registered implementation of _IRestRulesProvider_.
_DefaultRestRulesProvider_ is a default implementation that can be used to provide a set of rules to be processed at each request, as close as the REST specification it can be.
This can be combined with default error codes (_GenericErrorCodes_) used for _OutcomeEntries_, which can be used to provide a standard way to handle errors in the API.
For example 401 error code, is used to return an Unauthorized error, 404 to return a Not Found error, and so on. This can be customized by implementing _IOutcomeEntryProvider_.

Using the RestMediator allows to avoid code repetition and condensate the logic in a handful of concise and clear lines of code.

### Query Handling
The query handling feature allows to retrieve data from the system, by using the Query pattern, where every query is represented as a Query object, and the Mediator is responsible for executing these objects.
Through the dependency injection configuration, the Query handlers are automatically registered in the DI container and decorated with the configured decorators.

### Command Handling
The command handling feature allows to execute actions in the system, by using the Command pattern, where every action is represented as a Command object, and the Mediator is responsible for executing these objects.
Through the dependency injection configuration, the Command handlers are automatically registered in the DI container and decorated with the configured decorators.

### Decorators Attribute Validation
When an instance of _MindedBuilder_ is created, any implementation of _IDecoratingAttributeValidator_, will be invoked to validate all filtered assemblies.
This feature can be used to enforce the utilization of a specific interface along with the utilization of an attribute used for a given decorator.
For example, the _CachingDecorator_ requires the utilization of the _IGenerateCacheKey_ to get a unique identifier for the query result cache key.
So if any command is using any attribute derived from _CacheAttribute_, like _MemoryCacheAttribute_, doesn't implement _IGenerateCacheKey_, the validation will fail and the application will not start.
This check, is only executed at the application startup on debug mode, to avoid performance issues in production.

### Exception Decorator
The exception decorator is a feature that allows to handle exceptions in a single place, by using the Decorator pattern, the exception decorator is responsible for catching exceptions and logging them.

### Logging Decorator

### Validation Decorator

### Caching Decorator

### Odata Query Extension
This extension available with the package [Minded.Extensions.OData](https://www.nuget.org/packages/Minded.Extensions.OData/), allows to use OData queries with the framework.
To avoid coupling and a direct dependency over the OData library, the extension allows to transform an odata query option into a compatible _IQuery_ object.

#### Traits and support
In ord to use the OData extension, the _IQuery_ object must implement the traits which wants to be supported:
 * _ICanCount_ - To support the $count option
 * _ICanExpand_ - To support the $expand option
 * _ICanFilter_ - To support the $filter option
 * _ICanOrderBy_ - To support the $orderby option
 * _ICanSkip_ - To support the $skip option
 * _ICanTop_ - To support the $top option

### How does it work

**The entry point (Mediator)**

The implementation of [IMediator](https://github.com/norcino/Minded/blob/master/Mediator/IMediator.cs) interface ([Mediator Design Pattern](https://en.wikipedia.org/wiki/Mediator_pattern)), is injected everywhere there is a dependency on business logic or where it is needed to retrieve data.

The Mediator allows to hide and decouple from the mechanism used to execute commands and queries, so called Handlers.


**The encapsulation of the business logic (Command Query)**

Every action is represented as a [ICommand](https://github.com/norcino/Minded/blob/master/CommandQuery/Command/ICommand.cs), every data read from the system is considered an [IQuery](https://github.com/norcino/Minded/blob/master/CommandQuery/Query/IQuery.cs) (from now on I will refer to Command as Command or Query).
Concepts are taken from ([Command Design Pattern](https://en.wikipedia.org/wiki/Command_pattern) and [Command Query](https://en.wikipedia.org/wiki/Command%E2%80%93query_separation)).

Using the mediator, the Command is passed for the execution, and following the [Dependency Injection Configuration](https://github.com/norcino/Minded/tree/master/Configuration), one or more [ICommandHandler](https://github.com/norcino/Minded/blob/master/CommandQuery/Command/ICommandHandler.cs) or [IQueryHandler](https://github.com/norcino/Minded/blob/master/CommandQuery/Query/IQueryHandler.cs), will be instantiated to process the command or the query.

Note that a single command can have multiple Handlers responsible to process it.


**The decoration (Decorators)**

When an Handler is instantiated, by default it is decorated with:
* Validation decorator
* Transaction handling decorator
* Exception handling decorator
* Logging command decorator

Each decorator represent a layer around the handler, like an onion, each layer has and must have a single responsibility and the order used to register them in the dependency injection, is important to drive the order of execution (First registered, First executed).


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
In your `Program.cs` file, you will register the Minded Framework in the DI container. The `AddMinded` extension method is used to configure the Minded Framework and it takes two parameters:

1. A function that filters the assemblies you want to scan for Handlers. The function takes an `AssemblyName` object and returns a boolean value. Return `true` for the assemblies you wish to include.

2. An action delegate that allows you to configure the Minded Framework. This delegate receives a `MindedBuilder` object that you can use to add Mediators, Handlers, and Decorators.

Here's an example of what this looks like:

```csharp
services.AddMinded(assembly => assembly.Name.StartsWith("Service."), b =>
{
    b.AddMediator();
    b.AddRestMediator(); // Available with Minded.Extensions.WebApi

    b.AddCommandValidationDecorator() // Execution order 1
    .AddCommandExceptionDecorator() // Execution order 2
    .AddCommandLoggingDecorator() // Execution order 3
    .AddCommandHandlers(); // Execution order 4

    b.AddQueryExceptionDecorator()
    .AddQueryLoggingDecorator()
    .AddQueryHandlers();
});
```
The AddMediator and AddRestMediator methods are used to add the Mediator and RestMediator services to the DI container.
The AddCommandValidationDecorator, AddCommandExceptionDecorator, and AddCommandLoggingDecorator methods are used to add decorators that provide validation, exception handling, and logging for your command handlers.
The AddCommandHandlers method scans the assemblies you specified for any classes that implement the ICommandHandler<> or ICommandHandler<,> interfaces and registers them in the DI container.
Similar to the command handlers, AddQueryExceptionDecorator, AddQueryLoggingDecorator, and AddQueryHandlers methods are used to add decorators and handlers for your queries.

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

### Caching decorator
The caching decorator allows to configure caching for query results.
The _IQuery_ must implement ```IGenerateCacheKey``` in order to provide a unique identifier.
Additionally ```IGlobalCacheKeyPrefixProvider``` can be implemented and registered in the dependency injection configuration to provide a prefix which can guarantee global uniqueness.
A common example is to provide the Tenant ID in a multitenant system.


