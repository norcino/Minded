# Minded Framework Changelog

All notable changes to this project will be documented in this file.

## 1.0.3 (2023-05-15)
Added caching support using decorators.

#### Added
* Added new __Minded.Extensions.Caching.Abstractions__ and __Minded.Extensions.Caching.Memory__ Nuget packages
* Added `MemoryCacheAttribute` to activate the `MemoryCacheQueryHandlerDecorator` as long as the `IQuery` implements also `IGenerateCacheKey`
* Added `IGlobalCacheKeyPrefixProvider` which can be used to control global generation of cache prefixe

## 1.0.2 (2023-03-14)

Primary stable version fit for production use.
Providers the features described in the documentation.

#### Added
* Added new __Minded.Extensions.WebApi__ Nuget package
* Introduced `IRestMediator` which allows to process queries and commands to automatically return the appropriate `ActionResult` calculated using rule processors
* Added new __Minded.Extensions.CQRS.OData__ Nuget package
* Added extension method `IQuery<T>.ApplyODataQueryOptions<T>(ODataQueryOptions)` to apply `ODataQueryOptions` to `IQuery`
* Added extension method `IQueryable<T>.ApplyODataQueryOptions<T>(ODataQueryOptions)` to apply `ODataQueryOptions` to `IQueryable`
* Added new __Minded.Extensions.CQRS.EntityFramework__ Nuget package
* Added extension method `IQuery<IEnumberable<T>>.ApplyQueryTo<T>(IQueryable<T>)` which applies directly query traits to an `IQueryable`
* Added extension method `IQuery<T>.ApplyQueryTo<T>(IQueryable<T>)` which applies directly query traits to an `IQueryable`
* Added `ICommandHandler<ICommand<TResult>, TResult>` to strongly type return value from command executions

#### Fixed

* Traits not supporting nullable value have been fixed

#### Changed

* Updated sample API project to use the new Minded.Extensions.WebApi

## 0.1.3 (2022-12-16)

Primary stable version fit for production use.
Providers the features described in the documentation.

#### Added

* Added `RestMediator` and rules processing system to automatically let mediator return the correct `IActionResult`
* Added `ICommandHandler<ICommand<TResult>, TResult>` to strongly type return value from command executions

#### Fixed

* Minor fixes

#### Changed

* Updated the example application to reflect latest changes
