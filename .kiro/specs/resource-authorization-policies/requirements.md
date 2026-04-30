# Requirements Document

## Introduction

This specification extends the Minded authorization system to support claim-based authorization predicates and resource-instance ownership checks. The current system evaluates only roles and permissions via `RequireRolesAttribute` and `RequirePermissionsAttribute`. This feature adds generic claims to `AuthorizationContext`, introduces a `ResourceAuthorizeAttribute` that dispatches a mediator query to verify resource-level access, defines OR/AND evaluation semantics between resource authorization and existing RBAC attributes, implements recursion prevention for nested mediator calls during authorization, and provides a context population design for non-HTTP scenarios.

## Glossary

- **Authorization_Context**: The `AuthorizationContext` class that represents the current caller's identity data, including principal status, roles, permissions, and claims.
- **Authorization_Context_Accessor**: The `IAuthorizationContextAccessor` interface that provides access to the current caller's Authorization_Context.
- **Claim**: A key-value pair representing a piece of identity data beyond roles and permissions (e.g., tenant identifier, region, clearance level, user identifier).
- **Resource_Authorize_Attribute**: A new `ResourceAuthorizeAttribute` that decorates commands and queries to require resource-instance authorization via a mediator query dispatch.
- **Authorization_Query**: A mediator query dispatched by the resource authorization decorator to determine whether the caller has access to a specific resource instance.
- **Authorization_Descriptor**: The `AuthorizationDescriptor` class that compiles attribute metadata for a request type, extended to include resource authorization clauses.
- **Authorization_Evaluator**: The `IRequestAuthorizationEvaluator` implementation that evaluates role and permission clauses against the Authorization_Context.
- **Authorization_Decorator**: The command or query handler decorator that intercepts requests and evaluates authorization before delegating to the inner handler.
- **Minded_Context**: The `IMindedContext` ambient execution context that provides scoped values via `BeginScope`/`TryGetScoped` for async-flow-local state.
- **Minded_Context_Accessor**: The `IMindedContextAccessor` interface that provides access to the current Minded_Context.
- **Recursion_Bypass_Marker**: A private readonly struct used as a scoped value key in Minded_Context to prevent infinite recursion when the authorization decorator dispatches nested mediator calls.
- **OR_Clause**: One or more optional claim, role, or permission conditions (via `OrAnyRole`, `OrAnyPermission`, `OrAnyClaim` arrays) on an authorization attribute that, when any single condition is satisfied, short-circuits the primary check for that attribute entirely. Supported on Resource_Authorize_Attribute, RequireRolesAttribute, RequirePermissionsAttribute, and RequireClaimAttribute.
- **Descriptor_Cache**: The `AuthorizationDescriptorCache` that compiles and caches attribute metadata per request type using a thread-safe ConcurrentDictionary.

## Requirements

### Requirement 1: Extend AuthorizationContext with Claims

**User Story:** As a framework consumer, I want the AuthorizationContext to carry generic key-value claims beyond roles and permissions, so that authorization predicates can evaluate arbitrary identity data such as tenant identifiers, regions, and clearance levels.

#### Acceptance Criteria

1. THE Authorization_Context SHALL expose a `Claims` property of type `IReadOnlyDictionary<string, string>` that contains the caller's claims as key-value pairs.
2. WHEN an Authorization_Context is constructed without a claims parameter, THE Authorization_Context SHALL default the Claims property to an empty dictionary.
3. WHEN an Authorization_Context is constructed with a claims dictionary, THE Authorization_Context SHALL store the provided claims as a read-only dictionary accessible via the Claims property.
4. THE Authorization_Context SHALL preserve backward compatibility by keeping the existing constructor signature functional with an optional claims parameter.
5. WHEN a claim key is looked up in the Claims dictionary, THE Authorization_Context SHALL use case-insensitive key comparison consistent with the existing case-insensitive matching for roles and permissions.

### Requirement 2: ResourceAuthorizeAttribute Declaration

**User Story:** As a developer, I want to declare resource-instance authorization requirements on commands and queries using a `ResourceAuthorizeAttribute`, so that I can specify which property contains the resource identifier, which claim provides the caller's identity value, and which query type to dispatch for the access check.

#### Acceptance Criteria

1. THE Resource_Authorize_Attribute SHALL accept a `resourceIdProperty` parameter that names the property on the command or query containing the resource identifier.
2. THE Resource_Authorize_Attribute SHALL accept a `resourceIdClaim` parameter that names the claim key from the Authorization_Context to pass to the Authorization_Query.
3. THE Resource_Authorize_Attribute SHALL accept a `queryType` parameter that specifies the Type of the Authorization_Query to dispatch via the mediator.
4. THE Resource_Authorize_Attribute SHALL support `AllowMultiple = true` so that multiple resource authorization checks can be declared on a single command or query.
5. THE Resource_Authorize_Attribute SHALL support an optional `OrAnyRole` property of type `string[]` that specifies role names which can short-circuit the resource authorization check.
6. THE Resource_Authorize_Attribute SHALL support an optional `OrAnyPermission` property of type `string[]` that specifies permission names which can short-circuit the resource authorization check.
7. THE Resource_Authorize_Attribute SHALL support an optional `OrAnyClaim` property of type `string[]` that specifies claim keys which can short-circuit the resource authorization check.
8. WHEN a Resource_Authorize_Attribute is declared with `OrAnyRole`, THE Authorization_Decorator SHALL skip the resource authorization query dispatch if the caller possesses any of the specified roles.
9. WHEN a Resource_Authorize_Attribute is declared with `OrAnyPermission`, THE Authorization_Decorator SHALL skip the resource authorization query dispatch if the caller possesses any of the specified permissions.
10. WHEN a Resource_Authorize_Attribute is declared with `OrAnyClaim`, THE Authorization_Decorator SHALL skip the resource authorization query dispatch if the caller possesses any of the specified claim keys.
11. WHEN multiple OR arrays are specified on a single Resource_Authorize_Attribute, THE Authorization_Decorator SHALL treat them as a combined OR — if any single condition across all arrays is satisfied, the query dispatch is skipped.

### Requirement 3: Resource Authorization Query Dispatch

**User Story:** As a framework consumer, I want the authorization decorator to dispatch a mediator query to verify resource-level access, so that ownership and instance-level checks are evaluated as part of the authorization pipeline.

#### Acceptance Criteria

1. WHEN a command or query carries a Resource_Authorize_Attribute, THE Authorization_Decorator SHALL read the resource identifier value from the property named by `resourceIdProperty` on the request object.
2. WHEN a command or query carries a Resource_Authorize_Attribute, THE Authorization_Decorator SHALL read the claim value from the Authorization_Context Claims dictionary using the key specified by `resourceIdClaim`.
3. WHEN both the resource identifier and the claim value are resolved, THE Authorization_Decorator SHALL instantiate the Authorization_Query specified by `queryType` and dispatch it through the mediator.
4. WHEN the Authorization_Query returns a result indicating access is granted (Query returns true or QueryResponse with false value), THE Authorization_Decorator SHALL allow the request to proceed to the next handler.
5. WHEN the Authorization_Query returns a result indicating access is denied (Query returns false or QueryResponse with false value), THE Authorization_Decorator SHALL deny the request with a 403 Forbidden response.
6. IF the resource identifier property does not exist on the request type, THEN THE Descriptor_Cache SHALL throw an InvalidOperationException during attribute compilation at startup.
7. IF the claim key specified by `resourceIdClaim` is not present in the Authorization_Context Claims dictionary, THEN THE Authorization_Decorator SHALL deny the request with a 403 Forbidden response.

### Requirement 4: OR/AND Evaluation Semantics

**User Story:** As a developer, I want clear evaluation semantics when combining resource authorization with role and permission attributes, so that I can predictably control access using both coarse-grained and fine-grained checks.

#### Acceptance Criteria

1. WHEN multiple attributes (Resource_Authorize_Attribute, RequireRolesAttribute, RequirePermissionsAttribute, RequireClaimAttribute) are declared on a single command or query, THE Authorization_Decorator SHALL require all attributes to pass (AND semantics between attributes), unless an OR condition on an attribute is satisfied.
2. WHEN a Resource_Authorize_Attribute declares `OrAnyRole`, THE Authorization_Decorator SHALL evaluate the OR role conditions first before dispatching the Authorization_Query.
3. WHEN a Resource_Authorize_Attribute declares `OrAnyPermission`, THE Authorization_Decorator SHALL evaluate the OR permission conditions first before dispatching the Authorization_Query.
4. WHEN a Resource_Authorize_Attribute declares `OrAnyClaim`, THE Authorization_Decorator SHALL evaluate the OR claim conditions first before dispatching the Authorization_Query.
5. WHEN any OR condition across `OrAnyRole`, `OrAnyPermission`, or `OrAnyClaim` on any authorization attribute is satisfied, THE Authorization_Evaluator SHALL skip the primary check for that attribute and treat the attribute as passed.
6. WHEN no OR condition on a Resource_Authorize_Attribute is satisfied (or no OR arrays are specified), THE Authorization_Decorator SHALL proceed to dispatch the Authorization_Query and use its result to determine whether the attribute passes.
7. WHEN multiple Resource_Authorize_Attributes are declared on a single request, THE Authorization_Decorator SHALL evaluate each attribute independently and require all to pass (AND semantics).
8. WHEN a RequireRolesAttribute, RequirePermissionsAttribute, or RequireClaimAttribute declares OR conditions, THE Authorization_Evaluator SHALL evaluate the OR conditions first; if any OR condition is satisfied, the primary clause check for that attribute is skipped and the attribute is treated as passed.

### Requirement 5: Recursion Prevention for Authorization Query Dispatch

**User Story:** As a framework author, I want the authorization decorator to prevent infinite recursion when dispatching authorization queries through the mediator, so that nested mediator calls from the authorization check do not re-enter the authorization decorator indefinitely.
This feature leverage the `Minded.Extensions.Context` package in which `Readme.md` file it is possible to find additional information.

#### Acceptance Criteria

1. THE Authorization_Decorator SHALL declare a private readonly struct Recursion_Bypass_Marker type for use as a scoped value key in Minded_Context.
2. WHEN the Authorization_Decorator begins processing a request, THE Authorization_Decorator SHALL check Minded_Context for an active Recursion_Bypass_Marker scope using `TryGetScoped`.
3. WHEN a Recursion_Bypass_Marker scope is active, THE Authorization_Decorator SHALL skip all authorization logic and delegate directly to the next handler.
4. WHEN the Authorization_Decorator dispatches an Authorization_Query through the mediator, THE Authorization_Decorator SHALL open a Recursion_Bypass_Marker scope using `BeginScope` around the mediator call only.
5. WHEN the Authorization_Query mediator call completes, THE Authorization_Decorator SHALL close the Recursion_Bypass_Marker scope before delegating to the next handler, so that business logic handlers receive full decorator coverage.
6. WHEN two authorization queries are dispatched concurrently from parallel branches via `Task.WhenAll`, THE Recursion_Bypass_Marker scope in one branch SHALL NOT be visible to the other branch.

### Requirement 6: AuthorizationDescriptor Extension for Resource Clauses

**User Story:** As a framework author, I want the AuthorizationDescriptor and its cache to compile and store resource authorization metadata alongside existing claim, role and permission clauses, so that the decorator has a single compiled descriptor per request type.

#### Acceptance Criteria

1. THE Authorization_Descriptor SHALL expose a `ResourceClauses` property containing the compiled metadata from all Resource_Authorize_Attribute instances on the request type.
2. WHEN a request type has no Resource_Authorize_Attribute, THE Authorization_Descriptor SHALL set ResourceClauses to an empty read-only collection.
3. WHEN a request type has one or more Resource_Authorize_Attributes, THE Descriptor_Cache SHALL compile each attribute into a resource clause containing the resource identifier property name, the claim key, the query type, and the optional OR arrays (`OrAnyRole`, `OrAnyPermission`, `OrAnyClaim`).
4. THE Descriptor_Cache SHALL validate Resource_Authorize_Attribute configurations at startup, verifying that the `resourceIdProperty` exists on the request type and that the `queryType` implements the expected query interface.
5. WHEN a Resource_Authorize_Attribute is present on a request type, THE Authorization_Descriptor SHALL set `IsProtected` to true.

### Requirement 7: Context Population for Non-HTTP Scenarios

**User Story:** As a developer building workers, background services, or desktop applications, I want a mechanism to populate the AuthorizationContext with claims without relying on an HTTP context, so that the authorization system works consistently across all hosting environments.

#### Acceptance Criteria

1. THE Authorization_Context_Accessor SHALL remain an interface with no dependency on HTTP-specific types, allowing implementations for any hosting environment.
2. WHEN a consumer registers an Authorization_Context_Accessor implementation for a non-HTTP scenario (worker, service, desktop app), THE Authorization_Context_Accessor SHALL provide an Authorization_Context populated with claims from the environment-specific identity source.
3. THE framework SHALL document the contract that Authorization_Context_Accessor implementations must populate the Claims dictionary from their environment-specific identity source (e.g., service account tokens, certificate claims, desktop identity providers).
4. WHEN no Authorization_Context_Accessor implementation is registered, THE Authorization_Decorator SHALL treat the absence of a principal as an unauthenticated request.

### Requirement 8: Startup Validation of ResourceAuthorizeAttribute

**User Story:** As a developer, I want invalid ResourceAuthorizeAttribute configurations to be detected at application startup, so that misconfigured authorization attributes fail fast with clear error messages.

#### Acceptance Criteria

1. WHEN a Resource_Authorize_Attribute references a `resourceIdProperty` that does not exist on the request type, THEN THE AttributeValidator SHALL throw an InvalidOperationException with a message identifying the request type and the missing property name.
2. WHEN a Resource_Authorize_Attribute specifies a `queryType` that does not implement the expected query interface, THEN THE AttributeValidator SHALL throw an InvalidOperationException with a message identifying the request type and the invalid query type.
3. WHEN a Resource_Authorize_Attribute is combined with AllowUnauthenticatedAttribute on the same request type, THEN THE AttributeValidator SHALL throw an InvalidOperationException indicating the contradictory configuration.
4. THE AttributeValidator SHALL validate all Resource_Authorize_Attribute configurations eagerly during service registration, consistent with the existing eager validation pattern for RequireRolesAttribute and RequirePermissionsAttribute.

### Requirement 9: Authorization Query Contract

**User Story:** As a developer, I want a clear contract for authorization queries dispatched by the ResourceAuthorizeAttribute, so that I can implement resource-level access checks with a consistent interface.

#### Acceptance Criteria

1. THE Authorization_Query SHALL accept the resource identifier value and the claim value as constructor parameters.
2. THE Authorization_Query SHALL return a boolean result indicating whether access is granted.
3. WHEN the Authorization_Decorator instantiates an Authorization_Query, THE Authorization_Decorator SHALL pass the resource identifier value extracted from the request and the claim value extracted from the Authorization_Context.
4. THE Authorization_Query SHALL be a standard mediator query implementing the framework's `IQuery<bool>` or `IQuery<IQueryResponse<bool>>` interface, allowing it to be handled by a regular query handler with full decorator pipeline support (except for the bypassed authorization decorator via recursion prevention).

### Requirement 10: Add RequireClaimAttribute

**User Story:** As a developer, I want to declare claim-based authorization requirements on commands and queries using a `RequireClaimAttribute`, so that I can enforce claim predicates (e.g., tenant, region, clearance level) using the same matching semantics already established for roles and permissions.

#### Acceptance Criteria for Requirement 10

1. THE `RequireClaimAttribute` SHALL accept a `claimType` parameter that names the claim key to evaluate from the Authorization_Context Claims dictionary, and a `values` parameter that specifies the allowed claim values.
2. THE `RequireClaimAttribute` SHALL support a `Match` property of type `AuthorizationMatch` (All, Any, AtLeast, None) with a default of `All`, consistent with RequireRolesAttribute and RequirePermissionsAttribute.
3. THE `RequireClaimAttribute` SHALL support a `Minimum` property for use with `AuthorizationMatch.AtLeast`, consistent with RequireRolesAttribute and RequirePermissionsAttribute.
4. THE `RequireClaimAttribute` SHALL support `AllowMultiple = true` so that multiple claim checks can be declared on a single command or query, combined with implicit AND semantics between attributes.
5. WHEN a command or query carries a `RequireClaimAttribute` with `values` specified, THE Authorization_Evaluator SHALL look up the claim key in the Authorization_Context Claims dictionary and evaluate the claim value against the specified allowed values using the configured `AuthorizationMatch` mode.
6. WHEN the claim key specified by `claimType` is not present in the Authorization_Context Claims dictionary, THE Authorization_Evaluator SHALL treat the clause as not satisfied (deny).
7. WHEN claim values are compared, THE Authorization_Evaluator SHALL use case-insensitive, trimmed string comparison consistent with the existing matching for roles and permissions.
8. THE Authorization_Descriptor SHALL expose a `ClaimClauses` property containing the compiled metadata from all `RequireClaimAttribute` instances on the request type.
9. THE Descriptor_Cache SHALL compile each `RequireClaimAttribute` into a claim clause containing the claim type, the allowed values, the match mode, the minimum count, and the optional `MatchProperty` name.
10. THE AttributeValidator SHALL validate `RequireClaimAttribute` configurations at startup: when `values` are specified, apply the same rules as RequireRolesAttribute and RequirePermissionsAttribute (non-empty values, no blanks, no duplicates, valid Minimum for AtLeast mode); when `MatchProperty` is specified, verify the property exists on the request type.
11. WHEN a `RequireClaimAttribute` is combined with AllowUnauthenticatedAttribute on the same request type, THE AttributeValidator SHALL throw an InvalidOperationException indicating the contradictory configuration.
12. WHEN a `RequireClaimAttribute` is present on a request type, THE Authorization_Descriptor SHALL set `IsProtected` to true.
13. THE `RequireClaimAttribute` SHALL support an optional `MatchProperty` property of type `string` that names a property on the command or query whose runtime value is compared against the claim value.
14. WHEN a `RequireClaimAttribute` specifies `MatchProperty`, THE Authorization_Decorator SHALL read the property value from the request object and compare it (case-insensitive, using `ToString()`) against the claim value from the Authorization_Context Claims dictionary.
15. WHEN a `RequireClaimAttribute` specifies `MatchProperty`, the `values` parameter SHALL be optional and ignored if provided.
16. WHEN a `RequireClaimAttribute` specifies `MatchProperty` and the claim value does not match the property value, THE Authorization_Decorator SHALL deny the request with a 403 Forbidden response.
17. THE AttributeValidator SHALL throw an InvalidOperationException at startup if a `RequireClaimAttribute` specifies a `MatchProperty` that does not exist on the request type.
18. THE AttributeValidator SHALL throw an InvalidOperationException at startup if a `RequireClaimAttribute` specifies neither `values` nor `MatchProperty`.

### Requirement 11: OR Clause Support on RequireRolesAttribute

**User Story:** As a developer, I want to declare alternative conditions on a `RequireRolesAttribute` using OR clauses, so that a role requirement can be bypassed if the caller possesses a specific permission or claim.

#### Acceptance Criteria for Requirement 11

1. THE `RequireRolesAttribute` SHALL support an optional `OrAnyRole` property of type `string[]` that specifies alternative role names; if the caller possesses any of these roles, the primary role clause is skipped and the attribute is treated as passed.
2. THE `RequireRolesAttribute` SHALL support an optional `OrAnyPermission` property of type `string[]` that specifies alternative permission names; if the caller possesses any of these permissions, the primary role clause is skipped and the attribute is treated as passed.
3. THE `RequireRolesAttribute` SHALL support an optional `OrAnyClaim` property of type `string[]` that specifies alternative claim keys; if the caller possesses any of these claim keys in the Authorization_Context Claims dictionary, the primary role clause is skipped and the attribute is treated as passed.
4. WHEN multiple OR arrays are specified on a single `RequireRolesAttribute`, THE Authorization_Evaluator SHALL treat them as a combined OR — if any single condition across all arrays is satisfied, the primary role clause check is skipped.
5. WHEN no OR condition is satisfied (or no OR arrays are specified), THE Authorization_Evaluator SHALL evaluate the primary role clause using the existing `AuthorizationMatch` logic.
6. THE AttributeValidator SHALL validate that OR array entries on `RequireRolesAttribute` contain no blank or whitespace-only strings.

### Requirement 12: OR Clause Support on RequirePermissionsAttribute

**User Story:** As a developer, I want to declare alternative conditions on a `RequirePermissionsAttribute` using OR clauses, so that a permission requirement can be bypassed if the caller possesses a specific role or claim.

#### Acceptance Criteria for Requirement 12

1. THE `RequirePermissionsAttribute` SHALL support an optional `OrAnyRole` property of type `string[]` that specifies alternative role names; if the caller possesses any of these roles, the primary permission clause is skipped and the attribute is treated as passed.
2. THE `RequirePermissionsAttribute` SHALL support an optional `OrAnyPermission` property of type `string[]` that specifies alternative permission names; if the caller possesses any of these permissions, the primary permission clause is skipped and the attribute is treated as passed.
3. THE `RequirePermissionsAttribute` SHALL support an optional `OrAnyClaim` property of type `string[]` that specifies alternative claim keys; if the caller possesses any of these claim keys in the Authorization_Context Claims dictionary, the primary permission clause is skipped and the attribute is treated as passed.
4. WHEN multiple OR arrays are specified on a single `RequirePermissionsAttribute`, THE Authorization_Evaluator SHALL treat them as a combined OR — if any single condition across all arrays is satisfied, the primary permission clause check is skipped.
5. WHEN no OR condition is satisfied (or no OR arrays are specified), THE Authorization_Evaluator SHALL evaluate the primary permission clause using the existing `AuthorizationMatch` logic.
6. THE AttributeValidator SHALL validate that OR array entries on `RequirePermissionsAttribute` contain no blank or whitespace-only strings.

### Requirement 13: OR Clause Support on RequireClaimAttribute

**User Story:** As a developer, I want to declare alternative conditions on a `RequireClaimAttribute` using OR clauses, so that a claim requirement can be bypassed if the caller possesses a specific role or permission.

#### Acceptance Criteria for Requirement 13

1. THE `RequireClaimAttribute` SHALL support an optional `OrAnyRole` property of type `string[]` that specifies alternative role names; if the caller possesses any of these roles, the primary claim clause is skipped and the attribute is treated as passed.
2. THE `RequireClaimAttribute` SHALL support an optional `OrAnyPermission` property of type `string[]` that specifies alternative permission names; if the caller possesses any of these permissions, the primary claim clause is skipped and the attribute is treated as passed.
3. THE `RequireClaimAttribute` SHALL support an optional `OrAnyClaim` property of type `string[]` that specifies alternative claim keys; if the caller possesses any of these claim keys in the Authorization_Context Claims dictionary, the primary claim clause is skipped and the attribute is treated as passed.
4. WHEN multiple OR arrays are specified on a single `RequireClaimAttribute`, THE Authorization_Evaluator SHALL treat them as a combined OR — if any single condition across all arrays is satisfied, the primary claim clause check is skipped.
5. WHEN no OR condition is satisfied (or no OR arrays are specified), THE Authorization_Evaluator SHALL evaluate the primary claim clause using the existing `AuthorizationMatch` logic.
6. THE AttributeValidator SHALL validate that OR array entries on `RequireClaimAttribute` contain no blank or whitespace-only strings.
