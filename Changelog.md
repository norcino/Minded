# Minded Framework Changelog

All notable changes to this project will be documented in this file.

## Unreleased

#### Added

* Splitted projects and configuration of multiple NuGet packages

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
