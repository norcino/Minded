# Minded Framework Changelog

All notable changes to this project will be documented in this file.

## 1.2.0 (2025-11-17)
Added sensitive data protection feature to prevent PII and confidential data from appearing in logs.
Created new `Minded.Extensions.DataProtection` packages for centralized data protection functionality.
Added `[SensitiveData]` attribute to mark properties containing sensitive information.
Added configuration options to control sensitive data visibility with provider pattern support.

### New Packages

* **Minded.Extensions.DataProtection.Abstractions** - Interfaces and attributes for data protection
* **Minded.Extensions.DataProtection** - Concrete implementations of data sanitization

### Affected

* Minded.Extensions.Logging - Now uses DataProtection packages (optional dependency)
* Minded.Extensions.Exception - Now uses DataProtection packages (optional dependency, removed dependency on Logging)

### Added

* Added `Minded.Extensions.DataProtection.Abstractions` package with core abstractions
* Added `Minded.Extensions.DataProtection` package with concrete implementations
* Added `[SensitiveData]` attribute to mark properties containing PII or confidential business data
* Added `IDataSanitizer` interface for inspecting and sanitizing objects before logging
* Added `DataSanitizer` implementation with reflection-based property inspection
* Added `NullDataSanitizer` no-op implementation for when DataProtection is not configured
* Added `DataProtectionOptions` configuration class with `ShowSensitiveData` and `ShowSensitiveDataProvider`
* Added `AddDataProtection()` extension method on `MindedBuilder` for easy configuration
* Added `AddDataProtection<TImplementation>()` for custom sanitizer implementations
* Added automatic data sanitization in `LoggingCommandHandlerDecorator` and `LoggingQueryHandlerDecorator`
* Added automatic data sanitization in `ExceptionCommandHandlerDecorator` and `ExceptionQueryHandlerDecorator`
* Added `DiagnosticDataSanitizer` to remove non-serializable types and excluded properties before applying IDataSanitizer
* Added `[ExcludeFromSerializedDiagnosticLogging]` attribute to mark properties that should never appear in exception logs
* Added protection against infinite recursion with max depth limit (3 levels)
* Added collection truncation in logs (max 10 items) to prevent excessive log size

### Changed

* Moved `IDataSanitizer` from `Minded.Extensions.Logging` to `Minded.Extensions.DataProtection.Abstractions`
* Moved `SensitiveDataAttribute` from `Minded.Extensions.Logging` to `Minded.Extensions.DataProtection.Abstractions`
* Moved `DataSanitizer` from `Minded.Extensions.Logging` to `Minded.Extensions.DataProtection`
* Removed dependency from `Minded.Extensions.Exception` to `Minded.Extensions.Logging`
* Updated all logging decorators to sanitize commands/queries before logging (when DataProtection is configured)
* Updated all exception decorators to sanitize commands/queries before JSON serialization (when DataProtection is configured)
* Properties marked with `[SensitiveData]` are now omitted from logs by default
* Both Logging and Exception packages now work without DataProtection installed (using `NullDataSanitizer`)

### Security

* Sensitive data is now hidden by default in logs for GDPR/CCPA compliance
* To show sensitive data (e.g., in development), configure DataProtection with `ShowSensitiveData = true` or use `ShowSensitiveDataProvider`

### Example Usage

```csharp
using Minded.Extensions.DataProtection.Abstractions;

// Mark sensitive properties
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }

    [SensitiveData]
    public string Email { get; set; }

    [SensitiveData]
    public string Surname { get; set; }
}

// Configure DataProtection to show sensitive data in development only
services.AddMinded(Configuration, assembly => assembly.Name.StartsWith("Service."), builder =>
{
    builder.AddDataProtection(options =>
    {
        options.ShowSensitiveDataProvider = () => _environment.IsDevelopment();
    });

    builder.AddLogging();
    builder.AddExceptionHandling();
});

// Or use a custom sanitizer implementation
services.AddMinded(Configuration, assembly => assembly.Name.StartsWith("Service."), builder =>
{
    builder.AddDataProtection<MyCustomDataSanitizer>();
    builder.AddLogging();
});
```

## 1.1.0 (2025-11-16)
Added comprehensive CancellationToken support throughout the entire CQRS pipeline.
Added proper handling of OperationCanceledException to return appropriate HTTP status codes.
Added automatic database seeding for development and debugging environments.
Added comprehensive Retry decorator for handling transient failures in commands and queries.
Added extensive unit tests and integration tests for retry functionality.
Added outcome entry logging to Logging decorator with configurable severity filtering.
Added dynamic provider pattern for all LoggingOptions properties to support feature flags.
Fixed critical bugs in DefaultRulesProcessor and validation logic.

### Affected

* Minded.Framework.CQRS.Abstractions
* Minded.Framework.Mediator.Abstractions
* Minded.Framework.Mediator
* Minded.Extensions.Caching.Memory
* Minded.Extensions.Exception
* Minded.Extensions.Logging
* Minded.Extensions.Validation
* Minded.Extensions.WebApi
* **NEW** Minded.Extensions.Transaction (now fully implemented)
* **NEW** Minded.Extensions.Retry
* Minded.Extensions.WebApi
* Example.Application.Api
* Example.Data.Context
* All Example Service Handlers

### Added

* Added `CancellationToken cancellationToken = default` parameter to `ICommandHandler.HandleAsync()` methods
* Added `CancellationToken cancellationToken = default` parameter to `IQueryHandler.HandleAsync()` method
* Added `CancellationToken cancellationToken = default` parameter to all `IMediator` methods
* Added `CancellationToken cancellationToken = default` parameter to all `IRestMediator` methods
* Added special handling for `OperationCanceledException` in Exception decorators (logs as Information, not Error)
* Added `OperationCanceledException` handling in `RestMediator` to return HTTP 499 (Client Closed Request)
* Added `DatabaseSeeder` class for automatic database seeding in development environments
* Added automatic database seeding for SQLiteInMemory, LocalDb, and SQL Server (development only)
* **NEW Package**: `Minded.Extensions.Retry` - Retry decorator for transient failure handling
* Added `RetryCommandAttribute` to mark commands requiring retry logic with configurable retry count and delay intervals
* Added `RetryQueryAttribute` to mark queries requiring retry logic with configurable retry count and delay intervals
* Added `RetryOptions` configuration class for default retry settings (retry count, delays, ApplyToAllQueries)
* Added `RetryCommandHandlerDecorator<TCommand>` for commands without result
* Added `RetryCommandHandlerDecorator<TCommand, TResult>` for commands with result
* Added `RetryQueryHandlerDecorator<TQuery, TResult>` for queries
* Added `ServiceCollectionExtensions` with `AddCommandRetryDecorator()` and `AddQueryRetryDecorator()` methods
* Added support for up to 5 configurable delay intervals for exponential backoff patterns
* Added `ApplyToAllQueries` option to apply retry logic to all queries without requiring attribute
* Added fallback delay logic - if fewer delays than retries, last delay value is reused
* Added detailed logging of retry attempts with exception details
* Added 44 comprehensive unit tests for retry decorator functionality
* Added 5 integration tests for end-to-end retry decorator validation
* Added retry decorator demonstration in Example application (CreateCategoryCommandHandler)
* Added comprehensive retry decorator documentation in README.md
* Added `ExistsCategoryByIdQuery` and `ExistsTransactionByIdQuery` for data validation
* Added 48 edge case tests for DefaultRulesProcessor
* Added 23 unit tests for Service.Transaction
* Added 14 new unit tests for Service.Category (total 21)
* Added `LogOutcomeEntries` property to `LoggingOptions` to enable/disable outcome entry logging
* Added `MinimumOutcomeSeverityLevel` property to `LoggingOptions` for severity-based filtering (Error, Warning, Info)
* Added `EnabledProvider` function property to `LoggingOptions` for dynamic logging on/off control
* Added `LogMessageTemplateDataProvider` function property to `LoggingOptions` for dynamic template data logging control
* Added `LogOutcomeEntriesProvider` function property to `LoggingOptions` for dynamic outcome entry logging control
* Added `MinimumOutcomeSeverityLevelProvider` function property to `LoggingOptions` for dynamic severity level control
* Added `GetEffectiveEnabled()`, `GetEffectiveLogMessageTemplateData()`, `GetEffectiveLogOutcomeEntries()`, and `GetEffectiveMinimumSeverityLevel()` methods to `LoggingOptions`
* Added outcome entry logging in `LoggingCommandHandlerDecorator` with severity filtering
* Added outcome entry logging in `LoggingQueryHandlerDecorator` with severity filtering
* Added 11 new unit tests for LoggingOptions provider functionality
* **NEW Feature**: Fully implemented Transaction decorator for automatic transaction management
* Added `TransactionOptions` configuration class with properties: `DefaultTransactionScopeOption`, `DefaultIsolationLevel`, `DefaultTimeout`, `RollbackOnUnsuccessfulResponse`, `EnableLogging`
* Added `TransactionalCommandHandlerDecorator<TCommand>` for commands without result
* Added `TransactionalCommandHandlerDecorator<TCommand, TResult>` for commands with result
* Added `TransactionalQueryHandlerDecorator<TQuery, TResult>` for queries (with warnings about limited use cases)
* Added `ServiceCollectionExtensions` with `AddCommandTransactionDecorator()` and `AddQueryTransactionDecorator()` methods
* Added support for nested transaction handling with `TransactionScopeOption` (Required, RequiresNew, Suppress)
* Added support for configurable isolation levels (ReadCommitted, RepeatableRead, Serializable, etc.)
* Added per-command/query timeout configuration via `TimeoutSeconds` property in attributes
* Added automatic rollback on exception or unsuccessful response (configurable)
* Added comprehensive logging of transaction lifecycle (start, commit, rollback)
* Added automatic isolation level conflict resolution (creates new transaction when isolation levels differ)
* Enhanced `TransactionCommandAttribute` with `TimeoutSeconds` property and comprehensive XML documentation
* Enhanced `TransactionQueryAttribute` with `TimeoutSeconds` property and comprehensive XML documentation
* Updated `TransactionManager` with modern async/await support and comprehensive logging methods
* Added comprehensive transaction decorator documentation in README.md with examples, limitations, and best practices

### Changed

* All decorator base classes now accept and propagate `CancellationToken`
* All decorator implementations (Validation, Exception, Logging, Transaction, Caching) now support `CancellationToken`
* All example command and query handlers updated to accept and use `CancellationToken`
* `appsettings.Development.json` now uses `SQLiteInMemory` by default for easier debugging
* Exception decorators now distinguish between cancellations (Information log) and real errors (Error log)
* `RestMediator` now returns HTTP 499 for cancelled requests instead of HTTP 500
* Updated Example application to demonstrate retry logic with simulated transient failures
* Updated `CreateCategoryCommandHandler` to fail 3 times before succeeding (retry demonstration)
* Updated README.md with retry decorator documentation, usage examples, and best practices
* Updated Available Packages table to include Minded.Extensions.Retry
* Updated `LoggingCommandHandlerDecorator` to use `GetEffective*()` methods for all configuration checks
* Updated `LoggingQueryHandlerDecorator` to use `GetEffective*()` methods for all configuration checks
* Updated `ServiceCollectionExtensions` documentation with comprehensive examples showing static, dynamic, and mixed configuration patterns
* Removed vendor-specific references (LaunchDarkly) from documentation in favor of generic "feature flags" terminology
* Updated README.md with corrected decorator order explanation (innermost to outermost registration)
* Updated README.md with comprehensive "Understanding Decorator Order" section explaining registration vs execution order
* Updated README.md with recommended decorator orders for different scenarios (stateful vs stateless validation, with/without transactions)
* Updated `Minded.Extensions.Transaction.csproj` to target `netstandard2.0` and `net8.0` (was `net6.0`)
* Added package references to Transaction project: `Microsoft.Extensions.Configuration.Abstractions`, `Microsoft.Extensions.Options.ConfigurationExtensions`
* Added project reference to `Minded.Extensions.Configuration` in Transaction project
* Updated `TransactionOptions` to use type aliases to avoid namespace conflicts with `System.Transactions.TransactionOptions`
* Updated all transaction decorators to use fully qualified `Configuration.TransactionOptions` type

### Fixed

* Fixed missing `CancellationToken` propagation through decorator chain
* Fixed `OperationCanceledException` being logged as errors instead of information
* Fixed cancelled requests returning HTTP 500 instead of appropriate status code
* Fixed empty database when debugging with in-memory database
* **CRITICAL**: Fixed NullReferenceException in `DefaultRulesProcessor.GetActionResult()` when processing null command results
* **CRITICAL**: Fixed NullReferenceException in `DefaultRulesProcessor.GetActionResult()` when processing null query results
* Fixed null checking bug in `UpdateCategoryCommandValidator` where validation was performed after accessing potentially null property
* Fixed Moq extension method mocking issue in tests by using MockQueryable.Moq library's GetMockDbSet pattern

## 1.0.9 (2024-12-19)
Added constraint causing startup error in Debug, when a query doesn't implement IGenerateCacheKey.
Added IDecoratingAttributeValidator to support validation of attributes required for specific decorators.
Added fixes in various packages.

### Affected
* Minded.Extensions.Caching.Memory
* Minded.Extensions.Validation
* Minded.Extensions.Configuration
* Minded.Extensions.WebApi
* Minded.Framework.Mediator

### Added
* Added IDecoratingAttributeValidator to support validation of attributes required for specific decorators

### Changed
* If query or command are not successful the result is not cached

### Fixed
* Fixed issue with Validation decorator not returning the correct result when the query was not successful.
* Fixed Caching Decorator used in conjunction with IQueryResult<>.
* Fixed issue with RestOperations comparison in the RuleProcessor.
* Fixed Mediator not handling correctly null result when return type was ICommandResponse<>.


## 1.0.8 (2024-12-10)
Changed memory query decorator to not save if operation unsuccessful.
Added support to return Outcome entries within the query handler decorator.

### Affected
* Minded.Extensions.Validation
* Minded.Extensions.Caching.Memory
* Minded.Extensions.Configuration

### Changed
* If query or command are not successful the result is not cached


## 1.0.7 (2024-12-06)
Added IQueryResponse support, upgraded to .net 8

### Affected
* Minded.Extensions.Validation
* Minded.Extensions.Logging

### Added
* TypeHelper class to help with types operations
* IQueryResponse to mimic ICommandResponse and IMessageResponse
* Added ValidatingQueryHandlerDecorator with ValidateQuery attribute to validate queries like already available for Commands

### Changed
* Updated packages to target .net 8 instead of .net 6 which is out of support
* ICommandResponse now implements IMessageResponse where the properties are defined


## 1.0.6 (2023-07-13)
Fixed issues introduced in previous versions.

### Affected
* Minded.Extensions.Validation
* Minded.Extensions.Logging

### Changed
* Fixed logging decorator failing when a command was using the new ICommandHandler<C,R>
* Fixed validation decorator failing when using ICommandHandler without result type with a successful validation


## 1.0.5 (2023-07-10)
Added LinkSource to all packages and updated Logging configuration setup.

### Affected
* All

### Changed

* Logging Decorator allows semplified configuration passing the Type of the `IGlobalCacheKeyPrefixProvider`.
* Configuration method AddMinded accepts `IConfiguration` to make it available to extension methods used to setup decorators.


## 1.0.4 (2023-07-03)
Logging Extension has been refactored to reduce the amount of code needed when creating commands and queries in order to be logged.

### Affected
* Minded.Extensions.Caching.Memory
* Minded.Extensions.Caching.Abstractions
* Minded.Extensions.Logging
* Minded.Extensions.Validation
* Minded.Extensions.Validation.Abstractions
* Minded.Extensions.WebApi
* Minded.Framework.CQRS
* Minded.Framework.CQRS.Abstractions

### Added
* `IMessage` to encapsulate properties which are used in `IQuery` and `ICommand`, this interface is not meant to be used directly
* `LoggingOptions` to configure the logging Decorator
    ````json
    "Minded": {
        "LoggingOptions": {
            // Enables logging of all processed Queries and Commands
            "Enabled": true,
            // Logs messages data as defined in ILoggable
            "LogMessageTemplateData": false
        }
    }
    ````

### Changed

* Renamed Caching Extention `NullGlobalCacheKeyPrefixProvider` in `EmptyGlobalCacheKeyPrefixProvider` (no changes needed)
* `ILoggableCommand` and `ILoggableQuery` have been removed and all messages will be logged automatiocally.
* `ILoggable` interface has been updated adding properties to define template and parameters, it can be optionally implemented to log details about the query and commands
* `LogEvent` and `LogInfo` have been removed
* Logging decorator can be configured using `LoggingOptions`


## 1.0.3 (2023-05-15)
Added caching support using decorators.

### Added
* Added new __Minded.Extensions.Caching.Abstractions__ and __Minded.Extensions.Caching.Memory__ Nuget packages
* Added `MemoryCacheAttribute` to activate the `MemoryCacheQueryHandlerDecorator` as long as the `IQuery` implements also `IGenerateCacheKey`
* Added `IGlobalCacheKeyPrefixProvider` which can be used to control global generation of cache prefixe


## 1.0.2 (2023-03-14)

Primary stable version fit for production use.
Providers the features described in the documentation.

### Added
* Added new __Minded.Extensions.WebApi__ Nuget package
* Introduced `IRestMediator` which allows to process queries and commands to automatically return the appropriate `ActionResult` calculated using rule processors
* Added new __Minded.Extensions.CQRS.OData__ Nuget package
* Added extension method `IQuery<T>.ApplyODataQueryOptions<T>(ODataQueryOptions)` to apply `ODataQueryOptions` to `IQuery`
* Added extension method `IQueryable<T>.ApplyODataQueryOptions<T>(ODataQueryOptions)` to apply `ODataQueryOptions` to `IQueryable`
* Added new __Minded.Extensions.CQRS.EntityFramework__ Nuget package
* Added extension method `IQuery<IEnumberable<T>>.ApplyQueryTo<T>(IQueryable<T>)` which applies directly query traits to an `IQueryable`
* Added extension method `IQuery<T>.ApplyQueryTo<T>(IQueryable<T>)` which applies directly query traits to an `IQueryable`
* Added `ICommandHandler<ICommand<TResult>, TResult>` to strongly type return value from command executions

### Fixed

* Traits not supporting nullable value have been fixed

### Changed

* Updated sample API project to use the new Minded.Extensions.WebApi


## 0.1.3 (2022-12-16)

Primary stable version fit for production use.
Providers the features described in the documentation.

### Added

* Added `RestMediator` and rules processing system to automatically let mediator return the correct `IActionResult`
* Added `ICommandHandler<ICommand<TResult>, TResult>` to strongly type return value from command executions

### Fixed

* Minor fixes

### Changed

* Updated the example application to reflect latest changes
