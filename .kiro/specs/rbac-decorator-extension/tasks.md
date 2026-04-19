# Implementation Plan: RBAC Decorator Extension

## Overview

Implement a Role-Based Access Control (RBAC) decorator extension for the Minded framework. The extension adds authorization enforcement to the command/query decorator pipeline through attributes, a pluggable evaluator, and an abstracted authentication context. Implementation follows the existing decorator patterns (Logging, Validation, Exception, Transaction) and uses C# with MSTest + FsCheck for property-based tests.

## Tasks

- [x] 1. Create project structure and core types
  - [x] 1.1 Create the `Extensions/Minded.Extensions.Authorization/Minded.Extensions.Authorization.csproj` project file targeting `netstandard2.0;net8.0;net10.0`, referencing `Minded.Framework.Decorator`, `Minded.Extensions.Configuration`, `Minded.Extensions.Exception`, and `Microsoft.Extensions.Options.ConfigurationExtensions`
    - Follow the same csproj structure as `Minded.Extensions.Logging.csproj`
    - _Requirements: 22.1_

  - [x] 1.2 Create the `AuthorizationMatch` enum in `Extensions/Minded.Extensions.Authorization/Attributes/AuthorizationMatch.cs` with values `All`, `Any`, `AtLeast`, `None`
    - Place in `Minded.Extensions.Authorization.Attributes` namespace
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 22.2_

  - [x] 1.3 Create `RequireRolesAttribute` in `Extensions/Minded.Extensions.Authorization/Attributes/RequireRolesAttribute.cs`
    - `[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]`
    - Constructor accepts `params string[] roles`, exposes `Roles`, `Match` (default `All`), `Minimum` (default `0`)
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 22.2_

  - [x] 1.4 Create `RequirePermissionsAttribute` in `Extensions/Minded.Extensions.Authorization/Attributes/RequirePermissionsAttribute.cs`
    - Same pattern as `RequireRolesAttribute` but with `Permissions` property
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 22.2_

  - [x] 1.5 Create `RequireAuthenticationAttribute` in `Extensions/Minded.Extensions.Authorization/Attributes/RequireAuthenticationAttribute.cs`
    - `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]`, no constructor parameters
    - _Requirements: 26.1, 26.2, 26.3, 26.4, 26.5_

  - [x] 1.6 Create `AllowUnauthenticatedAttribute` in `Extensions/Minded.Extensions.Authorization/Attributes/AllowUnauthenticatedAttribute.cs`
    - `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]`, no constructor parameters
    - _Requirements: 23.1, 23.2, 23.3, 23.4, 23.5_

- [x] 2. Implement context and evaluation types
  - [x] 2.1 Create `AuthorizationContext` class in `Extensions/Minded.Extensions.Authorization/AuthorizationContext.cs`
    - Constructor with `bool hasPrincipal`, optional `IReadOnlyCollection<string> roles`, optional `IReadOnlyCollection<string> permissions`
    - Null roles/permissions default to `Array.Empty<string>()`
    - _Requirements: 6.2, 6.3, 6.4, 6.5, 6.6_

  - [x] 2.2 Create `IAuthorizationContextAccessor` interface in `Extensions/Minded.Extensions.Authorization/IAuthorizationContextAccessor.cs`
    - Single `Current` property of type `AuthorizationContext`
    - _Requirements: 6.1, 6.7, 6.8, 6.9_

  - [x] 2.3 Create `AuthorizationDecision` class and internal `AuthorizationDecisionReason` enum in `Extensions/Minded.Extensions.Authorization/AuthorizationDecision.cs`
    - `Allowed` bool property, internal `Reason` property
    - Static factory methods: `Allow()`, `Deny()`, `NoPrincipal()`
    - _Requirements: 7.3, 7.4_

  - [x] 2.4 Create `AuthorizationDescriptor`, `RoleClause`, and `PermissionClause` in `Extensions/Minded.Extensions.Authorization/AuthorizationDescriptor.cs`
    - All immutable after construction
    - Descriptor exposes `IsProtected`, `AllowUnauthenticated`, `RequireAuthenticationOnly`, `RoleClauses`, `PermissionClauses`
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8_

  - [x] 2.5 Create `IRequestAuthorizationEvaluator` interface in `Extensions/Minded.Extensions.Authorization/IRequestAuthorizationEvaluator.cs`
    - `Evaluate(Type requestType, AuthorizationDescriptor descriptor, AuthorizationContext context)` returning `AuthorizationDecision`
    - _Requirements: 7.1, 7.2_

  - [x] 2.6 Create `DefaultRequestAuthorizationEvaluator` in `Extensions/Minded.Extensions.Authorization/DefaultRequestAuthorizationEvaluator.cs`
    - Check `HasPrincipal` first → `NoPrincipal()` if false
    - If `RequireAuthenticationOnly` with no RBAC clauses → `Allow()`
    - Evaluate all role clauses (implicit AND), then all permission clauses (implicit AND)
    - Match modes: `All` = subset, `Any` = intersection, `AtLeast` = minimum count, `None` = disjointness
    - Use `StringComparer.OrdinalIgnoreCase` with trimmed values
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 7.5, 21.1, 21.2, 21.3_

- [x] 3. Create test project and write property tests for evaluator
  - [x] 3.1 Create `Tests/Minded.Extensions.Authorization.Tests/Minded.Extensions.Authorization.Tests.csproj` MSTest project
    - Reference `Minded.Extensions.Authorization`, `Minded.Framework.CQRS`, `Minded.Framework.Decorator`
    - Add FsCheck NuGet package version to `Directory.Packages.props` and reference `FsCheck` in the test csproj
    - Include packages: `MSTest.TestFramework`, `MSTest.TestAdapter`, `Microsoft.NET.Test.Sdk`, `Moq`, `FluentAssertions`, `AnonymousData`, `FsCheck`
    - _Requirements: 22.1_

  - [x] 3.2 Write property tests for `DefaultRequestAuthorizationEvaluator` in `Tests/Minded.Extensions.Authorization.Tests/Evaluation/DefaultRequestAuthorizationEvaluatorTests.cs`
    - **Property 1: Match.All evaluates as subset check**
    - **Property 2: Match.Any evaluates as intersection check**
    - **Property 3: Match.AtLeast evaluates as minimum count check**
    - **Property 4: Match.None evaluates as disjointness check**
    - **Property 5: Case and whitespace normalization does not affect evaluation**
    - **Property 6: Multiple clauses combine with implicit AND**
    - Use `Prop.ForAll(...).QuickCheckThrowOnFailure()` inside `[TestMethod]` methods
    - Minimum 100 iterations per property
    - **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 5.1, 5.2, 5.3, 5.4, 7.5, 21.1, 21.2, 21.3**

  - [x] 3.3 Write property test for `AuthorizationContext` in `Tests/Minded.Extensions.Authorization.Tests/Context/AuthorizationContextTests.cs`
    - **Property 7: AuthorizationContext collections are never null**
    - **Validates: Requirements 6.3, 6.4, 6.5, 6.6**

- [x] 4. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Implement descriptor cache and attribute validation
  - [x] 5.1 Create `AuthorizationDescriptorCache` as internal static class in `Extensions/Minded.Extensions.Authorization/Decorator/AuthorizationDescriptorCache.cs`
    - `ConcurrentDictionary<Type, AuthorizationDescriptor>` for thread-safe caching
    - `GetOrCreate(Type requestType)` method that compiles descriptor from `TypeDescriptor.GetAttributes()`
    - Compile logic: read `RequireRolesAttribute`, `RequirePermissionsAttribute`, `RequireAuthenticationAttribute`, `AllowUnauthenticatedAttribute`
    - Set `IsProtected = true` if any RBAC or RequireAuthentication attribute present
    - Set `AllowUnauthenticated = true` if `AllowUnauthenticatedAttribute` present
    - Set `RequireAuthenticationOnly = true` if `RequireAuthenticationAttribute` present without RBAC clauses
    - Unattributed types get `IsProtected = false`
    - _Requirements: 9.1, 9.2, 9.3, 9.4_

  - [x] 5.2 Implement attribute validation logic (can be a static helper or validator class)
    - Validate: empty/null item arrays, blank/whitespace items, duplicates after case-insensitive trim, `AtLeast` with `Minimum <= 0` or `Minimum > count`, non-`AtLeast` with `Minimum != 0`, `AllowUnauthenticated` + RBAC attribute conflict
    - Throw `InvalidOperationException` for each violation
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 23.6_

  - [x] 5.3 Write property tests for descriptor cache and attribute validation
    - **Property 8: Descriptor compilation is correct and deterministic** in `Tests/Minded.Extensions.Authorization.Tests/Descriptors/AuthorizationDescriptorCacheTests.cs`
    - **Property 9: Descriptor cache returns same instance per type** in `Tests/Minded.Extensions.Authorization.Tests/Descriptors/AuthorizationDescriptorCacheTests.cs`
    - **Property 10: Invalid attribute configurations are rejected at validation** in `Tests/Minded.Extensions.Authorization.Tests/Descriptors/AttributeValidationTests.cs`
    - **Validates: Requirements 8.1, 8.7, 8.8, 9.1, 9.2, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 23.6**

- [x] 6. Implement command authorization decorators
  - [x] 6.1 Create `AuthorizationCommandHandlerDecorator<TCommand>` in `Extensions/Minded.Extensions.Authorization/Decorator/AuthorizationCommandHandlerDecorator.cs`
    - Extend `CommandHandlerDecoratorBase<TCommand>`, implement `ICommandHandler<TCommand>`
    - Constructor: inner handler, `IAuthorizationContextAccessor`, `IRequestAuthorizationEvaluator`, `IOptions<AuthorizationOptions>`, `ILogger`
    - HandleAsync flow: resolve descriptor → check if protected/enforce-auth → AllowUnauthenticated bypass → get context → evaluate → deny with `CommandResponse.Error(outcomeEntry)` using `GenericErrorCodes.NotAuthenticated` (401) or `GenericErrorCodes.NotAuthorized` (403) → or pass through
    - Deny-by-default: wrap entire auth flow in try-catch, deny on any exception
    - Log authorization outcome with request type, allowed/denied, duration; never log role/permission details
    - _Requirements: 10.1, 10.2, 10.4, 10.5, 12.1, 12.2, 12.5, 12.6, 12.7, 13.1, 13.2, 13.3, 13.4, 14.1, 14.2, 14.3, 20.1, 20.2, 20.3, 20.4, 20.5, 20.6, 25.1, 25.3, 25.4_

  - [x] 6.2 Create `AuthorizationCommandHandlerDecorator<TCommand, TResult>` in the same file or a separate file
    - Same pattern for `ICommand<TResult>`, returns `ICommandResponse<TResult>`
    - Deny returns `CommandResponse<TResult>.Error(outcomeEntry)`
    - _Requirements: 10.3_

  - [x] 6.3 Write property tests for command decorators in `Tests/Minded.Extensions.Authorization.Tests/Decorators/CommandAuthorizationDecoratorTests.cs`
    - **Property 11: Authorized requests pass through unchanged**
    - **Property 12: Denied commands return unsuccessful response with correct error code**
    - **Property 15: Denied requests never invoke the inner handler**
    - **Property 16: Unauthenticated callers on envelope responses get 401**
    - **Property 18: Denial OutcomeEntry contains no detail leakage**
    - **Property 19: Unprotected requests pass through without checks**
    - **Property 20: Enforce-authentication policy denies unattributed unauthenticated requests**
    - **Property 21: AllowUnauthenticatedAttribute bypasses all checks under enforce-auth**
    - **Property 22: RequireAuthenticationAttribute with principal passes through without RBAC**
    - **Validates: Requirements 10.1, 10.2, 10.3, 10.4, 10.5, 12.1, 12.2, 12.5, 12.6, 12.7, 13.1, 13.2, 13.3, 13.4, 25.1, 25.3, 25.4, 26.7**

- [x] 7. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. Implement query authorization decorator
  - [x] 8.1 Create `AuthorizationQueryHandlerDecorator<TQuery, TResult>` in `Extensions/Minded.Extensions.Authorization/Decorator/AuthorizationQueryHandlerDecorator.cs`
    - Extend `QueryHandlerDecoratorBase<TQuery, TResult>`, implement `IQueryHandler<TQuery, TResult>`
    - Same auth flow as command decorator
    - For denied queries: if `TResult` implements `IQueryResponse<>` → return `QueryResponse<T>.Error(outcomeEntry)`; if raw type → throw `System.Security.SecurityException` (403) or `System.UnauthorizedAccessException` (401)
    - Deny-by-default, logging, no detail leakage — same rules as command decorator
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 12.1, 12.3, 12.4, 12.6, 13.1, 13.2, 13.3, 13.4, 14.1, 14.2, 14.3, 15.1, 15.2, 15.3, 15.4, 20.1, 20.2, 20.3, 20.4, 20.5, 20.6, 25.2, 25.3, 25.4_

  - [x] 8.2 Write property tests for query decorator in `Tests/Minded.Extensions.Authorization.Tests/Decorators/QueryAuthorizationDecoratorTests.cs`
    - **Property 11: Authorized requests pass through unchanged**
    - **Property 13: Denied queries with IQueryResponse return unsuccessful response with correct error code**
    - **Property 14: Denied raw queries throw SecurityException**
    - **Property 15: Denied requests never invoke the inner handler**
    - **Property 16: Unauthenticated callers on envelope responses get 401**
    - **Property 17: Unauthenticated callers on raw queries throw UnauthorizedAccessException**
    - **Property 18: Denial OutcomeEntry contains no detail leakage**
    - **Property 19: Unprotected requests pass through without checks**
    - **Property 20: Enforce-authentication policy denies unattributed unauthenticated requests**
    - **Property 21: AllowUnauthenticatedAttribute bypasses all checks under enforce-auth**
    - **Validates: Requirements 11.1, 11.2, 11.3, 11.4, 11.5, 12.3, 12.4, 12.6, 13.1, 13.2, 13.3, 13.4, 15.1, 15.2, 25.2, 25.3, 25.4**

- [x] 9. Implement configuration and registration
  - [x] 9.1 Create `AuthorizationOptions` in `Extensions/Minded.Extensions.Authorization/Configuration/AuthorizationOptions.cs`
    - `RequireAuthenticationForAllCommands` (bool, default false) + `Func<bool>` provider
    - `RequireAuthenticationForAllQueries` (bool, default false) + `Func<bool>` provider
    - `GetEffectiveRequireAuthenticationForAllCommands()` and `GetEffectiveRequireAuthenticationForAllQueries()` methods
    - Follow same pattern as `LoggingOptions`
    - _Requirements: 24.1, 24.2, 24.3, 24.4_

  - [x] 9.2 Create `ServiceCollectionExtensions` in `Extensions/Minded.Extensions.Authorization/Configuration/ServiceCollectionExtensions.cs`
    - `AddCommandAuthorizationDecorator(this MindedBuilder, Action<AuthorizationOptions> configure = null)` — registers command decorators for both `ICommand` and `ICommand<TResult>` via `QueueCommandDecoratorRegistrationAction` and `QueueCommandWithResultDecoratorRegistrationAction`, configures options, registers default `IRequestAuthorizationEvaluator`, eagerly validates attributes
    - `AddQueryAuthorizationDecorator(this MindedBuilder, Action<AuthorizationOptions> configure = null)` — same for query decorators via `QueueQueryDecoratorRegistrationAction`
    - `AddAuthorizationContextAccessor<TAccessor>(this IServiceCollection)` — registers scoped `IAuthorizationContextAccessor`
    - `AddRequestAuthorizationEvaluator<TEvaluator>(this IServiceCollection)` — registers singleton `IRequestAuthorizationEvaluator`
    - All builder methods return `MindedBuilder` for fluent chaining
    - _Requirements: 17.1, 17.2, 17.3, 17.4, 17.5, 17.6, 17.7, 17.8, 18.1, 18.2_

  - [x] 9.3 Write property test for `AuthorizationOptions` in `Tests/Minded.Extensions.Authorization.Tests/Configuration/AuthorizationOptionsTests.cs`
    - **Property 23: AuthorizationOptions GetEffective resolves provider or falls back**
    - **Validates: Requirements 24.4**

  - [x] 9.4 Write unit tests for registration API in `Tests/Minded.Extensions.Authorization.Tests/Configuration/ServiceCollectionExtensionsTests.cs`
    - Verify `AddCommandAuthorizationDecorator` and `AddQueryAuthorizationDecorator` register decorators and return builder
    - Verify `AddAuthorizationContextAccessor<T>` registers scoped accessor
    - Verify `AddRequestAuthorizationEvaluator<T>` registers singleton evaluator
    - Verify default evaluator is registered when no custom evaluator provided
    - _Requirements: 17.1, 17.2, 17.3, 17.4, 17.5_

- [x] 10. REST compatibility and logging verification
  - [x] 10.1 Write property test for REST error code compatibility in `Tests/Minded.Extensions.Authorization.Tests/Decorators/CommandAuthorizationDecoratorTests.cs` (or a shared test file)
    - **Property 24: REST error codes are compatible with DefaultRestRulesProvider**
    - **Validates: Requirements 19.1, 19.2, 19.3**

  - [x] 10.2 Write property test for authorization logging in `Tests/Minded.Extensions.Authorization.Tests/Logging/AuthorizationLoggingTests.cs`
    - **Property 25: Authorization logging includes type, outcome, and duration without detail leakage**
    - **Validates: Requirements 20.1, 20.2, 20.3, 20.4, 20.5**

- [x] 11. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 12. Add Authorization Decorator documentation to README.md
  - [x] 12.1 Add an "Authorization Decorator" section to the "Available Decorators" area in `README.md`, following the same style as the existing decorator sections (Exception, Validation, Retry, Logging, Caching, Transaction)
    - Include: **Purpose**, **Package** (`Minded.Extensions.Authorization`), **Installation** (`dotnet add package Minded.Extensions.Authorization`)
    - Include: **Usage** section showing `AddCommandAuthorizationDecorator()` and `AddQueryAuthorizationDecorator()` registration with recommended pipeline ordering
    - Include: **How it works** numbered list explaining the decorator flow (resolve descriptor → check authentication → evaluate RBAC → allow or deny)
    - Include: **Attribute examples** for all four attributes: `[RequireRoles]`, `[RequirePermissions]`, `[RequireAuthentication]`, `[AllowUnauthenticated]`
    - Include: **Match modes** section with examples for `All`, `Any`, `AtLeast`, `None`
    - Include: **Compound rules** example showing multiple attributes combined with implicit AND
    - Include: **Configuration** section showing `AuthorizationOptions` with `RequireAuthenticationForAllCommands`/`RequireAuthenticationForAllQueries` and the provider pattern, matching the existing "Decorator Configuration Options" convention
    - Include: **IAuthorizationContextAccessor** section showing how to implement the interface to bridge the consumer's authentication mechanism
    - Include: **Error codes** table: 401 (NotAuthenticated) for unauthenticated callers, 403 (NotAuthorized) for RBAC denials
    - Include: **Raw query exceptions** note: `System.Security.SecurityException` for 403, `System.UnauthorizedAccessException` for 401
    - Include: **Best Practices** section (use `[AllowUnauthenticated]` sparingly, enable enforce-auth policy for secure-by-default, place authorization after validation in the pipeline)
    - Follow the same markdown formatting, heading levels, and code block style as the existing decorator sections
  - [x] 12.2 Update the "Decorators with Configuration Options" table in the README to include the Authorization decorator with `AuthorizationOptions`, Providers support ✅, and appsettings.json support status
  - [x] 12.3 Update the Table of Contents if needed to include the new Authorization Decorator section
  - [x] 12.4 Update the "Available Packages" section to include `Minded.Extensions.Authorization`
  - [x] 12.5 Update the "Installation" section to include `dotnet add package Minded.Extensions.Authorization` in the extensions list

- [x] 13. Final documentation review checkpoint
  - Ensure all documentation is consistent with the existing README style, examples compile, and no broken links exist.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document using FsCheck with MSTest integration
- Unit tests validate specific examples and edge cases
- The design uses C# throughout, so no language selection was needed
- FsCheck package version needs to be added to `Directory.Packages.props` since it's not currently listed
- The authorization decorator sits after validation/transaction and before logging/exception in the pipeline per Requirement 18
