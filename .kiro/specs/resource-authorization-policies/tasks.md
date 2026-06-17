# Implementation Plan: Resource Authorization Policies

## Overview

This plan implements claim-based authorization predicates and resource-instance ownership checks for the Minded authorization system. The implementation proceeds incrementally: first extending the data model and context, then adding new attributes and descriptor compilation, then extending the evaluator with claim and OR-clause logic, then modifying the decorators for resource authorization with recursion prevention, and finally wiring everything together in DI registration. Each step builds on the previous and ends with integration into the existing pipeline.

## Tasks

- [ ] 1. Add project reference and extend AuthorizationContext with Claims
  - [ ] 1.1 Add Minded.Extensions.Context project reference to Minded.Extensions.Authorization.csproj and add Minded.Extensions.Context project reference to the test project csproj
    - Add `<ProjectReference Include="..\Minded.Extensions.Context\Minded.Extensions.Context.csproj" />` to `Extensions/Minded.Extensions.Authorization/Minded.Extensions.Authorization.csproj`
    - Add `<ProjectReference Include="..\..\Extensions\Minded.Extensions.Context\Minded.Extensions.Context.csproj" />` to `Tests/Minded.Extensions.Authorization.Tests/Minded.Extensions.Authorization.Tests.csproj`
    - _Requirements: 5.1, 5.4 (IMindedContextAccessor dependency needed for recursion prevention)_
  - [ ] 1.2 Extend AuthorizationContext with Claims property
    - Add `IReadOnlyDictionary<string, string> Claims` property to `AuthorizationContext`
    - Extend the constructor with an optional `claims` parameter defaulting to an empty `Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)`
    - Preserve backward compatibility: existing constructor calls without claims must still work
    - Add XML documentation on the new property and updated constructor parameter
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_
  - [ ]* 1.3 Write property tests for AuthorizationContext Claims
    - **Property 1: Claims construction round-trip** — for any dictionary of string key-value pairs, the Claims property returns exactly the same pairs
    - **Property 2: Case-insensitive claim key lookup** — for any claim key, lookup with any case variation returns the same value
    - **Validates: Requirements 1.3, 1.5**
  - [ ]* 1.4 Write unit tests for AuthorizationContext Claims
    - Test default constructor without claims parameter returns empty dictionary
    - Test constructor with null claims parameter returns empty dictionary
    - Test backward compatibility: existing 3-parameter constructor still works
    - Add tests to `Tests/Minded.Extensions.Authorization.Tests/Context/AuthorizationContextClaimsTests.cs`
    - _Requirements: 1.2, 1.4_

- [ ] 2. Create new attributes (RequireClaimAttribute, ResourceAuthorizeAttribute) and extend existing attributes with OR clauses
  - [ ] 2.1 Create RequireClaimAttribute
    - Create `Extensions/Minded.Extensions.Authorization/Attributes/RequireClaimAttribute.cs`
    - Constructor accepts `string claimType` and `params string[] values`
    - Properties: `ClaimType`, `Values`, `Match` (default `AuthorizationMatch.All`), `Minimum` (default 0), `MatchProperty` (optional string)
    - OR clause properties: `OrAnyRole`, `OrAnyPermission`, `OrAnyClaim` (all `string[]`)
    - `[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]`
    - XML documentation on all public members
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.13, 13.1, 13.2, 13.3_
  - [ ] 2.2 Create ResourceAuthorizeAttribute
    - Create `Extensions/Minded.Extensions.Authorization/Attributes/ResourceAuthorizeAttribute.cs`
    - Constructor accepts `string resourceIdProperty`, `string claimName`, `Type queryType`
    - OR clause properties: `OrAnyRole`, `OrAnyPermission`, `OrAnyClaim` (all `string[]`)
    - `[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]`
    - XML documentation on all public members
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7_
  - [ ] 2.3 Add OR clause properties to RequireRolesAttribute
    - Add `OrAnyRole`, `OrAnyPermission`, `OrAnyClaim` properties (all `string[]`) to existing `RequireRolesAttribute`
    - XML documentation on new properties
    - _Requirements: 11.1, 11.2, 11.3_
  - [ ] 2.4 Add OR clause properties to RequirePermissionsAttribute
    - Add `OrAnyRole`, `OrAnyPermission`, `OrAnyClaim` properties (all `string[]`) to existing `RequirePermissionsAttribute`
    - XML documentation on new properties
    - _Requirements: 12.1, 12.2, 12.3_

- [ ] 3. Extend AuthorizationDescriptor with ClaimClause, ResourceClause, and OR arrays on existing clauses
  - [ ] 3.1 Add ClaimClause and ResourceClause classes, extend RoleClause and PermissionClause with OR arrays
    - Add `ClaimClause` sealed class to `AuthorizationDescriptor.cs` with properties: `ClaimType`, `Values`, `Match`, `Minimum`, `MatchProperty`, `OrAnyRole`, `OrAnyPermission`, `OrAnyClaim`
    - Add `ResourceClause` sealed class to `AuthorizationDescriptor.cs` with properties: `ResourceIdProperty`, `ClaimName`, `QueryType`, `OrAnyRole`, `OrAnyPermission`, `OrAnyClaim`
    - Extend `RoleClause` with `OrAnyRole`, `OrAnyPermission`, `OrAnyClaim` properties (IReadOnlyList<string>)
    - Extend `PermissionClause` with `OrAnyRole`, `OrAnyPermission`, `OrAnyClaim` properties (IReadOnlyList<string>)
    - Extend `AuthorizationDescriptor` with `ClaimClauses` and `ResourceClauses` properties (IReadOnlyList), update constructor
    - XML documentation on all new types and members
    - _Requirements: 6.1, 6.2, 6.3, 6.5, 10.8, 10.9_
  - [ ]* 3.2 Write property test for descriptor compilation
    - **Property 9: Descriptor compilation preserves all attribute metadata** — for any request type with RequireClaimAttribute and/or ResourceAuthorizeAttribute, the compiled descriptor preserves all original attribute data
    - **Validates: Requirements 6.3, 10.9**

- [ ] 4. Extend AuthorizationDescriptorCache to compile new attributes
  - [ ] 4.1 Extend Compile method in AuthorizationDescriptorCache
    - Handle `RequireClaimAttribute`: compile into `ClaimClause` with claim type, values, match, minimum, matchProperty, and OR arrays
    - Handle `ResourceAuthorizeAttribute`: compile into `ResourceClause` with resourceIdProperty, claimName, queryType, and OR arrays
    - Compile OR arrays on `RequireRolesAttribute` and `RequirePermissionsAttribute` into the extended `RoleClause` and `PermissionClause`
    - Validate `resourceIdProperty` exists on request type during compilation
    - Validate `queryType` implements `IQuery<bool>` or `IQuery<IQueryResponse<bool>>` and has `(object, string)` constructor
    - Update `IsProtected` to include claim clauses and resource clauses
    - Update `RequireAuthenticationOnly` to account for claim and resource clauses
    - _Requirements: 6.3, 6.4, 6.5, 3.6, 8.1, 8.2_
  - [ ]* 4.2 Write unit tests for descriptor cache compilation of new attributes
    - Test RequireClaimAttribute compilation into ClaimClause
    - Test ResourceAuthorizeAttribute compilation into ResourceClause
    - Test OR arrays compiled on RoleClause and PermissionClause
    - Test IsProtected is true when claim or resource attributes are present
    - Test invalid resourceIdProperty throws InvalidOperationException
    - Test invalid queryType throws InvalidOperationException
    - Add tests to `Tests/Minded.Extensions.Authorization.Tests/Descriptors/`
    - _Requirements: 6.3, 6.4, 6.5_

- [ ] 5. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 6. Extend AttributeValidator for new attributes and OR arrays
  - [ ] 6.1 Add validation for RequireClaimAttribute, ResourceAuthorizeAttribute, and OR arrays on all attributes
    - Validate `RequireClaimAttribute`: non-empty claimType, valid values (non-empty, no blanks, no duplicates), valid Minimum for AtLeast, MatchProperty exists on request type if specified, either values or MatchProperty must be specified
    - Validate `ResourceAuthorizeAttribute`: non-blank resourceIdProperty, non-blank claimName, non-null queryType, OR arrays contain no blank entries
    - Validate OR arrays on `RequireRolesAttribute` and `RequirePermissionsAttribute`: no blank or whitespace-only entries
    - Validate `RequireClaimAttribute` + `AllowUnauthenticatedAttribute` is contradictory
    - Validate `ResourceAuthorizeAttribute` + `AllowUnauthenticatedAttribute` is contradictory
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 10.10, 10.11, 10.17, 10.18, 11.6, 12.6, 13.6_
  - [ ]* 6.2 Write unit tests for attribute validation of new attributes
    - Test RequireClaimAttribute with blank claimType throws
    - Test RequireClaimAttribute with empty values and no MatchProperty throws
    - Test RequireClaimAttribute with MatchProperty referencing non-existent property throws
    - Test ResourceAuthorizeAttribute with non-existent resourceIdProperty throws
    - Test ResourceAuthorizeAttribute with invalid queryType throws
    - Test ResourceAuthorizeAttribute + AllowUnauthenticated throws
    - Test RequireClaimAttribute + AllowUnauthenticated throws
    - Test OR arrays with blank entries throw on all attribute types
    - Add tests to `Tests/Minded.Extensions.Authorization.Tests/Descriptors/AttributeValidationResourceTests.cs`
    - _Requirements: 8.1, 8.2, 8.3, 10.10, 10.11, 10.17, 10.18_

- [ ] 7. Extend DefaultRequestAuthorizationEvaluator with claim evaluation and OR short-circuits
  - [ ] 7.1 Add claim clause evaluation and OR clause support to the evaluator
    - Add `IsOrClauseSatisfied` shared method that checks `OrAnyRole`, `OrAnyPermission`, `OrAnyClaim` against the `AuthorizationContext` (roles, permissions, claims)
    - Wrap existing role clause evaluation with OR short-circuit: if `IsOrClauseSatisfied` returns true, skip the primary role check
    - Wrap existing permission clause evaluation with OR short-circuit: if `IsOrClauseSatisfied` returns true, skip the primary permission check
    - Add claim clause evaluation loop: for each `ClaimClause` where `MatchProperty` is null, check OR short-circuit first, then evaluate claim value against allowed values using `AuthorizationMatch` semantics
    - `EvaluateClaimClause`: look up claim key in `context.Claims`, compare claim value against allowed values using case-insensitive trimmed comparison
    - Skip `ClaimClause` entries with `MatchProperty != null` (handled by decorator)
    - Update the `RequireAuthenticationOnly` pass-through check to include `ClaimClauses.Count == 0`
    - _Requirements: 4.5, 4.8, 10.5, 10.6, 10.7, 11.4, 11.5, 12.4, 12.5, 13.4, 13.5_
  - [ ]* 7.2 Write property tests for claim evaluation and OR short-circuits
    - **Property 10: Claim clause evaluation with match modes** — for any claim type, values, and AuthorizationMatch mode, the evaluator correctly determines pass/fail
    - **Property 11: OR clause satisfaction on RBAC attributes skips primary check** — for any attribute with OR clauses and a context satisfying any OR condition, the primary check is skipped
    - **Validates: Requirements 10.5, 10.7, 11.1-11.5, 12.1-12.5, 13.1-13.5**
  - [ ]* 7.3 Write unit tests for claim evaluation edge cases
    - Test claim key not present in context → deny
    - Test case-insensitive claim value comparison
    - Test All match mode with single claim value
    - Test Any match mode with multiple allowed values
    - Test AtLeast match mode
    - Test None match mode
    - Test OR clause with role match skips primary check
    - Test OR clause with permission match skips primary check
    - Test OR clause with claim key match skips primary check
    - Test combined OR arrays (role + permission + claim) — any single match skips
    - Add tests to `Tests/Minded.Extensions.Authorization.Tests/Evaluation/ClaimClauseEvaluationTests.cs`
    - _Requirements: 10.5, 10.6, 10.7_

- [ ] 8. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 9. Modify authorization decorators for bypass, MatchProperty claims, and resource clause evaluation
  - [ ] 9.1 Extend AuthorizationCommandHandlerDecorator (both variants) with IMindedContextAccessor, bypass marker, MatchProperty claim evaluation, and resource clause evaluation
    - Add `IMindedContextAccessor` constructor parameter to both `AuthorizationCommandHandlerDecorator<TCommand>` and `AuthorizationCommandHandlerDecorator<TCommand, TResult>`
    - Declare private `readonly struct AuthorizationBypass` in each decorator
    - At the top of `HandleAsync`, check `mindedContext.TryGetScoped<AuthorizationBypass>(out _)` — if active, delegate directly to next handler
    - After RBAC evaluation passes, evaluate `MatchProperty` claim clauses: for each ClaimClause with MatchProperty, check OR short-circuit, read claim value from context, read property value from request via reflection, compare case-insensitively
    - After MatchProperty claims pass, evaluate resource clauses: for each ResourceClause, check OR short-circuit, read resource ID from request, read claim value from context, open bypass scope, dispatch authorization query via mediator, close scope, check result
    - Handle both `IQuery<bool>` and `IQuery<IQueryResponse<bool>>` return types for authorization queries
    - Deny with 403 if claim key missing, property value mismatch, or query returns false
    - _Requirements: 2.8, 2.9, 2.10, 2.11, 3.1, 3.2, 3.3, 3.4, 3.5, 3.7, 5.1, 5.2, 5.3, 5.4, 5.5, 9.1, 9.2, 9.3, 9.4, 10.14, 10.15, 10.16_
  - [ ] 9.2 Extend AuthorizationQueryHandlerDecorator with IMindedContextAccessor, bypass marker, MatchProperty claim evaluation, and resource clause evaluation
    - Apply the same changes as 9.1 to `AuthorizationQueryHandlerDecorator<TQuery, TResult>`
    - Handle query-specific deny patterns: `IQueryResponse<T>` envelope → `QueryResponse<T>.Error(...)`, raw type → `SecurityException`/`UnauthorizedAccessException`
    - _Requirements: 2.8, 2.9, 2.10, 2.11, 3.1, 3.2, 3.3, 3.4, 3.5, 3.7, 5.1, 5.2, 5.3, 5.4, 5.5, 9.1, 9.2, 9.3, 9.4, 10.14, 10.15, 10.16_
  - [ ]* 9.3 Write property tests for resource authorization in decorators
    - **Property 3: OR clause satisfaction skips query dispatch** — for any context satisfying any OR condition on a ResourceAuthorizeAttribute, the mediator is NOT called
    - **Property 4: Authorization query receives correct resource ID and claim value** — the query is instantiated with the correct values from request and context
    - **Property 5: Decorator decision matches authorization query result** — allow iff query returns true
    - **Property 6: AND semantics across all attribute types** — request allowed only when ALL attributes pass
    - **Property 7: OR clause not satisfied triggers query dispatch** — when no OR condition is met, the query IS dispatched
    - **Property 8: Bypass marker skips all authorization** — when bypass scope is active, all auth logic is skipped
    - **Property 12: MatchProperty claim comparison uses request property value** — allow only when claim value equals request property value (case-insensitive)
    - **Validates: Requirements 2.8-2.11, 3.1-3.5, 4.1-4.7, 5.3, 10.14, 10.16**
  - [ ]* 9.4 Write unit tests for decorator resource authorization
    - Test resource clause evaluation: query dispatched and result used
    - Test missing claim key at runtime → 403
    - Test OR short-circuit with role → query not dispatched
    - Test OR short-circuit with permission → query not dispatched
    - Test OR short-circuit with claim key → query not dispatched
    - Test combined OR arrays — any single match skips query
    - Test multiple ResourceAuthorizeAttributes — all must pass (AND)
    - Test MatchProperty claim: matching value → allow
    - Test MatchProperty claim: non-matching value → 403
    - Test MatchProperty claim: missing claim key → 403
    - Test MatchProperty with OR short-circuit
    - Test bypass marker active → all auth skipped
    - Test bypass scope closed before delegating to next handler
    - Test IQuery<IQueryResponse<bool>> return type handling
    - Test null AuthorizationContext → 401
    - Test deny-by-default on exception in auth flow
    - Add tests to `Tests/Minded.Extensions.Authorization.Tests/Decorators/ResourceAuthorizationCommandDecoratorTests.cs` and `ResourceAuthorizationQueryDecoratorTests.cs`
    - _Requirements: 2.8, 2.9, 2.10, 3.1, 3.4, 3.5, 3.7, 4.1, 4.6, 4.7, 5.3, 5.5, 10.14, 10.16_

- [ ] 10. Extend ServiceCollectionExtensions for IMindedContextAccessor fallback and new attribute validation
  - [ ] 10.1 Update registration methods and eager validation
    - In `AddCommandAuthorizationDecorator` and `AddQueryAuthorizationDecorator`, register `IMindedContextAccessor` fallback via `TryAddSingleton<IMindedContextAccessor, NullMindedContextAccessor>` (from Minded.Extensions.Context)
    - Ensure eager validation includes `RequireClaimAttribute` and `ResourceAuthorizeAttribute` (already handled by updated `AttributeValidator.Validate`)
    - Add `using Minded.Extensions.Context;` to the file
    - _Requirements: 7.1, 7.4, 8.4_
  - [ ]* 10.2 Write unit tests for service registration with new attributes
    - Test IMindedContextAccessor fallback is registered when Context extension is not registered
    - Test eager validation catches invalid RequireClaimAttribute at startup
    - Test eager validation catches invalid ResourceAuthorizeAttribute at startup
    - Add tests to `Tests/Minded.Extensions.Authorization.Tests/Configuration/ServiceCollectionResourceAuthTests.cs`
    - _Requirements: 7.4, 8.4_

- [ ] 11. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 12. Final integration verification
  - [ ]* 12.1 Write recursion prevention integration tests
    - Test bypass marker is scoped to the dispatching async flow only
    - Test parallel authorization queries do not cross-contaminate bypass scopes
    - Add tests to `Tests/Minded.Extensions.Authorization.Tests/Decorators/RecursionPreventionTests.cs`
    - _Requirements: 5.3, 5.5, 5.6_
  - [ ]* 12.2 Write end-to-end integration tests combining all attribute types
    - Test request with RequireRoles + RequirePermissions + RequireClaim + ResourceAuthorize — all must pass
    - Test request with mixed OR clauses across attribute types
    - Test backward compatibility: existing RBAC-only requests behave identically
    - _Requirements: 4.1, 4.7, 7.1_

- [ ] 13. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document (12 properties)
- Unit tests validate specific examples and edge cases
- The design uses C# throughout — no language selection was needed
- All new public types and members require XML documentation per framework conventions
- The test project uses MSTest + FluentAssertions + FsCheck + Moq (existing conventions)
