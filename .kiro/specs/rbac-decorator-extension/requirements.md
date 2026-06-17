# Requirements Document

## Introduction

This document specifies the requirements for a V1 RBAC (Role-Based Access Control) decorator extension for the Minded framework. The extension allows commands and queries to declare required roles and permissions through attributes placed on the request type. At runtime, a decorator evaluates those requirements before invoking the handler, short-circuiting execution when authorization fails.

Because requiring roles or permissions inherently implies an authenticated caller, the decorator treats authentication as a prerequisite for authorization. When a protected request is processed without an authenticated principal, the decorator produces a distinct authentication failure (GenericErrorCodes.NotAuthenticated / HTTP 401) before any RBAC evaluation occurs. When an authenticated caller lacks the required roles or permissions, the decorator produces an authorization failure (GenericErrorCodes.NotAuthorized / HTTP 403). The authentication mechanism itself (JWT, cookies, API keys, etc.) is fully abstracted behind the IAuthorizationContextAccessor interface — the decorator only reads the result, never calls an auth provider directly.

V1 is intentionally limited to RBAC on request types. It does not include resource-based authorization, ownership checks, policy classes, tenant-aware rules, or expression parsing.

### Design Goals

- Feel native to Minded's existing decorator and attribute model
- Support simple and moderately complex RBAC rules without introducing a custom expression language
- Avoid leaking missing-role or missing-permission details in the response
- Short-circuit before handler execution
- Preserve strong typing for commands and queries
- Support future expansion without breaking the V1 attribute model
- Enable a secure-by-default posture where all requests require an intentional authorization decision when the enforce-authentication policy is enabled

### Non-Goals

- Policy-based authorization
- Claim predicates beyond roles and permissions
- Ownership or resource-instance checks
- Field-level or property-level authorization
- Authorization result localization
- Returning which specific role or permission failed
- Dynamic rule evaluation from a database
- Audit or event publishing requirements

## Glossary

- **Authorization_Decorator**: The Minded command or query handler decorator that intercepts protected requests and evaluates RBAC clauses before delegating to the inner handler
- **Protected_Request**: Any ICommand, ICommand\<TResult\>, IQuery\<TResult\>, or equivalent Minded request type decorated with one or more RBAC attributes (RequireRolesAttribute, RequirePermissionsAttribute, or RequireAuthenticationAttribute)
- **Unprotected_Request**: A Minded request type that carries no RBAC attributes and no AllowUnauthenticatedAttribute
- **Authorization_Clause**: One attribute instance (RequireRolesAttribute or RequirePermissionsAttribute) applied to a request type, representing a single RBAC condition
- **Authorization_Descriptor**: An internal immutable object compiled from the RBAC attributes on a request type, containing all role clauses and permission clauses; cached per request type
- **Authorization_Context**: A runtime view of the current caller, exposing HasPrincipal, Roles, and Permissions
- **Authorization_Context_Accessor**: The service (IAuthorizationContextAccessor) that provides the current Authorization_Context to the Authorization_Decorator; this is the sole authentication touchpoint and abstracts away the underlying authentication mechanism (JWT, cookies, API keys, etc.)
- **Authorization_Evaluator**: The service (IRequestAuthorizationEvaluator) that determines whether an Authorization_Context satisfies an Authorization_Descriptor
- **Authorization_Decision**: The immutable result of an evaluation, carrying an Allowed flag and an internal Reason enum for logging and testing
- **Authorization_Match**: An enum (All, Any, AtLeast, None) controlling how items within a single clause are matched
- **Role_Clause**: An immutable record within an Authorization_Descriptor representing one RequireRolesAttribute instance, containing the normalized role list, the match mode, and the minimum count
- **Permission_Clause**: An immutable record within an Authorization_Descriptor representing one RequirePermissionsAttribute instance, containing the normalized permission list, the match mode, and the minimum count
- **Envelope_Response**: A command response implementing ICommandResponse or a query response implementing IQueryResponse\<T\>, which carries a Successful flag and OutcomeEntries
- **Raw_Query_Result**: A query result type that does not implement IQueryResponse\<T\>
- **Descriptor_Cache**: A thread-safe, concurrent cache that stores Authorization_Descriptors keyed by request Type, ensuring each request type is compiled at most once
- **ForbiddenQueryException**: Replaced by System.Security.SecurityException — thrown by the Authorization_Decorator when a Raw_Query_Result query is denied authorization (403)
- **UnauthorizedException**: Replaced by System.UnauthorizedAccessException — thrown by the Authorization_Decorator when the caller has no principal (HasPrincipal is false) on a Protected_Request (401)
- **Deny_By_Default**: The principle that if descriptor resolution fails or the Authorization_Evaluator throws an unhandled exception, the Authorization_Decorator denies the request rather than allowing it through
- **MindedBuilder**: The Minded framework builder class used to register decorators, handlers, and services via fluent extension methods
- **OutcomeEntry**: A Minded framework class implementing IOutcomeEntry, carrying PropertyName, Message, ErrorCode, Severity, and AttemptedValue
- **GenericErrorCodes**: A Minded framework static class defining standard error code constants including NotAuthorized ("403") and NotAuthenticated ("401")
- **AuthorizationRestRulesProvider**: Not needed — the existing DefaultRestRulesProvider already maps GenericErrorCodes.NotAuthorized to HTTP 403 and GenericErrorCodes.NotAuthenticated to HTTP 401
- **AllowUnauthenticatedAttribute**: An attribute that explicitly marks a command or query as not requiring authentication, opting it out of the enforce-authentication-for-all policy when that policy is enabled
- **AuthorizationOptions**: A configuration class following Minded's options pattern that controls authorization decorator behavior, including the RequireAuthenticationForAllCommands and RequireAuthenticationForAllQueries settings
- **Enforce_Authentication_Policy**: When enabled via AuthorizationOptions, the policy that rejects any command or query that has neither RBAC attributes nor an AllowUnauthenticatedAttribute
- **RequireAuthenticationAttribute**: An attribute that marks a command or query as requiring an authenticated caller (HasPrincipal must be true) without imposing any specific role or permission requirements

## Requirements

### Requirement 1: RequireRolesAttribute Definition

**User Story:** As a developer, I want to place a RequireRolesAttribute on my command or query class, so that I can declaratively specify which roles are required to execute the request.

#### Acceptance Criteria

1. THE RequireRolesAttribute SHALL accept a params string array of role names in its constructor
2. THE RequireRolesAttribute SHALL expose a Match property of type AuthorizationMatch with a default value of All
3. THE RequireRolesAttribute SHALL expose a Minimum property of type int with a default value of 0
4. THE RequireRolesAttribute SHALL allow multiple instances on the same class (AllowMultiple = true)
5. THE RequireRolesAttribute SHALL be inherited by derived classes (Inherited = true)
6. THE RequireRolesAttribute SHALL target only classes (AttributeTargets.Class)

### Requirement 2: RequirePermissionsAttribute Definition

**User Story:** As a developer, I want to place a RequirePermissionsAttribute on my command or query class, so that I can declaratively specify which permissions are required to execute the request.

#### Acceptance Criteria

1. THE RequirePermissionsAttribute SHALL accept a params string array of permission names in its constructor
2. THE RequirePermissionsAttribute SHALL expose a Match property of type AuthorizationMatch with a default value of All
3. THE RequirePermissionsAttribute SHALL expose a Minimum property of type int with a default value of 0
4. THE RequirePermissionsAttribute SHALL allow multiple instances on the same class (AllowMultiple = true)
5. THE RequirePermissionsAttribute SHALL be inherited by derived classes (Inherited = true)
6. THE RequirePermissionsAttribute SHALL target only classes (AttributeTargets.Class)

### Requirement 3: AuthorizationMatch Enum

**User Story:** As a developer, I want to specify how roles or permissions within a single clause are matched, so that I can express All, Any, AtLeast, or None semantics.

#### Acceptance Criteria

1. THE AuthorizationMatch enum SHALL define the value All, meaning every item in the clause must be present
2. THE AuthorizationMatch enum SHALL define the value Any, meaning at least one item in the clause must be present
3. THE AuthorizationMatch enum SHALL define the value AtLeast, meaning at least Minimum items from the clause must be present
4. THE AuthorizationMatch enum SHALL define the value None, meaning none of the items in the clause may be present

### Requirement 4: Attribute Validation at Registration Time

**User Story:** As a developer, I want invalid attribute configurations to be detected at application startup, so that I receive immediate feedback rather than runtime surprises.

#### Acceptance Criteria

1. WHEN a RequireRolesAttribute or RequirePermissionsAttribute has an empty or null item array, THEN THE Authorization_Decorator registration SHALL throw an InvalidOperationException
2. WHEN a RequireRolesAttribute or RequirePermissionsAttribute contains a blank or whitespace-only item name, THEN THE Authorization_Decorator registration SHALL throw an InvalidOperationException
3. WHEN a RequireRolesAttribute or RequirePermissionsAttribute contains duplicate values after case-insensitive trimmed normalization, THEN THE Authorization_Decorator registration SHALL throw an InvalidOperationException
4. WHEN a RequireRolesAttribute or RequirePermissionsAttribute has Match set to AtLeast and Minimum is less than or equal to zero, THEN THE Authorization_Decorator registration SHALL throw an InvalidOperationException
5. WHEN a RequireRolesAttribute or RequirePermissionsAttribute has Match set to AtLeast and Minimum exceeds the item count, THEN THE Authorization_Decorator registration SHALL throw an InvalidOperationException
6. WHEN a RequireRolesAttribute or RequirePermissionsAttribute has Match set to a value other than AtLeast and Minimum is not zero, THEN THE Authorization_Decorator registration SHALL throw an InvalidOperationException

### Requirement 5: Normalization Rules

**User Story:** As a developer, I want role and permission names to be compared case-insensitively with whitespace trimmed, so that minor formatting differences do not cause authorization failures.

#### Acceptance Criteria

1. THE Authorization_Evaluator SHALL compare role names using StringComparer.OrdinalIgnoreCase
2. THE Authorization_Evaluator SHALL compare permission names using StringComparer.OrdinalIgnoreCase
3. THE Authorization_Evaluator SHALL trim leading and trailing whitespace from role names before comparison
4. THE Authorization_Evaluator SHALL trim leading and trailing whitespace from permission names before comparison
5. THE Authorization_Descriptor SHALL preserve original attribute values for diagnostic purposes only

### Requirement 6: Authorization Context Contract

**User Story:** As a developer, I want a well-defined authorization context interface that abstracts away the authentication mechanism, so that I can provide the current caller's identity information to the authorization system regardless of how authentication is performed.

#### Acceptance Criteria

1. THE IAuthorizationContextAccessor interface SHALL expose a Current property of type AuthorizationContext
2. THE AuthorizationContext SHALL expose a HasPrincipal property of type bool
3. THE AuthorizationContext SHALL expose a Roles property of type IReadOnlyCollection\<string\> that is never null
4. THE AuthorizationContext SHALL expose a Permissions property of type IReadOnlyCollection\<string\> that is never null
5. WHEN AuthorizationContext is constructed with a null Roles value, THE AuthorizationContext SHALL default Roles to an empty collection
6. WHEN AuthorizationContext is constructed with a null Permissions value, THE AuthorizationContext SHALL default Permissions to an empty collection
7. WHEN IAuthorizationContextAccessor.Current returns null, THEN THE Authorization_Decorator SHALL treat the caller as having no principal (HasPrincipal = false)
8. THE IAuthorizationContextAccessor SHALL be the only authentication touchpoint for the Authorization_Decorator; the decorator SHALL NOT depend on or invoke any authentication provider, middleware, or identity framework directly
9. THE IAuthorizationContextAccessor abstraction SHALL support any authentication mechanism (JWT, cookies, API keys, certificate-based, custom tokens) through consumer-provided implementations

### Requirement 7: Authorization Evaluator Contract

**User Story:** As a developer, I want a replaceable evaluator service, so that I can customize how authorization decisions are made without modifying the decorator.

#### Acceptance Criteria

1. THE IRequestAuthorizationEvaluator interface SHALL define an Evaluate method accepting a Type requestType, an AuthorizationDescriptor descriptor, and an AuthorizationContext context
2. THE IRequestAuthorizationEvaluator.Evaluate method SHALL return an AuthorizationDecision
3. THE AuthorizationDecision SHALL expose an Allowed property of type bool
4. THE AuthorizationDecision SHALL expose a Reason property of an internal enum type for logging and testing purposes, with values that distinguish at minimum between Allowed, Denied (RBAC clauses not satisfied), and NoPrincipal (unauthenticated caller)
5. THE default IRequestAuthorizationEvaluator implementation SHALL evaluate all role clauses and permission clauses, requiring every clause to pass (implicit AND across multiple attributes)

### Requirement 8: Authorization Descriptor and Clause Types

**User Story:** As a developer, I want the compiled attribute metadata to be represented as immutable, well-typed descriptors, so that the decorator can efficiently evaluate authorization rules.

#### Acceptance Criteria

1. THE AuthorizationDescriptor SHALL expose an IsProtected property of type bool indicating whether the request type has any RBAC attributes
2. THE AuthorizationDescriptor SHALL expose a RoleClauses property of type IReadOnlyList\<RoleClause\>
3. THE AuthorizationDescriptor SHALL expose a PermissionClauses property of type IReadOnlyList\<PermissionClause\>
4. THE RoleClause SHALL expose a Roles property of type IReadOnlyList\<string\>, a Match property of type AuthorizationMatch, and a Minimum property of type int
5. THE PermissionClause SHALL expose a Permissions property of type IReadOnlyList\<string\>, a Match property of type AuthorizationMatch, and a Minimum property of type int
6. THE AuthorizationDescriptor, RoleClause, and PermissionClause SHALL be immutable after construction
7. THE AuthorizationDescriptor SHALL expose an AllowUnauthenticated property of type bool indicating whether the request type carries the AllowUnauthenticatedAttribute
8. THE AuthorizationDescriptor SHALL expose a RequireAuthenticationOnly property of type bool indicating whether the request type carries the RequireAuthenticationAttribute without any RBAC clauses

### Requirement 9: Descriptor Caching

**User Story:** As a developer, I want authorization descriptors to be compiled once per request type and cached, so that repeated requests do not incur reflection overhead.

#### Acceptance Criteria

1. THE Descriptor_Cache SHALL store one AuthorizationDescriptor per request Type
2. THE Descriptor_Cache SHALL compile the descriptor at most once per request Type
3. THE Descriptor_Cache SHALL be thread-safe for concurrent reads and writes using ConcurrentDictionary or equivalent
4. WHEN a request type has no RBAC attributes, THE Descriptor_Cache SHALL store a descriptor with IsProtected set to false

### Requirement 10: Command Authorization Decorator Behavior

**User Story:** As a developer, I want the command authorization decorator to short-circuit denied commands with an unsuccessful response, so that the handler is never invoked for unauthorized callers.

#### Acceptance Criteria

1. WHEN a Protected_Request command is authorized, THE Authorization_Decorator SHALL invoke the inner command handler and return the original response
2. WHEN a Protected_Request command returning ICommandResponse is denied, THE Authorization_Decorator SHALL return an unsuccessful ICommandResponse with Successful set to false and exactly one OutcomeEntry with ErrorCode set to GenericErrorCodes.NotAuthorized
3. WHEN a Protected_Request command returning ICommandResponse\<TResult\> is denied, THE Authorization_Decorator SHALL return an unsuccessful ICommandResponse\<TResult\> with Successful set to false and exactly one OutcomeEntry with ErrorCode set to GenericErrorCodes.NotAuthorized
4. WHEN a Protected_Request command is denied, THE Authorization_Decorator SHALL NOT invoke the inner command handler
5. WHEN an Unprotected_Request command is processed, THE Authorization_Decorator SHALL pass through to the inner command handler without performing authorization checks

### Requirement 11: Query Authorization Decorator Behavior

**User Story:** As a developer, I want the query authorization decorator to short-circuit denied queries appropriately based on the return type, so that the handler is never invoked for unauthorized callers.

#### Acceptance Criteria

1. WHEN a Protected_Request query is authorized, THE Authorization_Decorator SHALL invoke the inner query handler and return the original result
2. WHEN a Protected_Request query returning IQueryResponse\<T\> is denied, THE Authorization_Decorator SHALL return an unsuccessful IQueryResponse\<T\> with Successful set to false and exactly one OutcomeEntry with ErrorCode set to GenericErrorCodes.NotAuthorized
3. WHEN a Protected_Request query returning a Raw_Query_Result is denied, THE Authorization_Decorator SHALL throw a System.Security.SecurityException
4. WHEN a Protected_Request query is denied, THE Authorization_Decorator SHALL NOT invoke the inner query handler
5. WHEN an Unprotected_Request query is processed, THE Authorization_Decorator SHALL pass through to the inner query handler without performing authorization checks

### Requirement 12: Authentication-First Evaluation and Unauthenticated Caller Handling

**User Story:** As a developer, I want the decorator to check authentication before authorization and produce distinct error codes for each, so that consumers and REST rules can differentiate between "who are you?" (401) and "you can't do that" (403).

#### Acceptance Criteria

1. WHEN a Protected_Request is processed, THE Authorization_Decorator SHALL check HasPrincipal BEFORE evaluating any RBAC clauses
2. WHEN HasPrincipal is false on a Protected_Request command returning an Envelope_Response, THE Authorization_Decorator SHALL return an unsuccessful response with exactly one OutcomeEntry with ErrorCode set to GenericErrorCodes.NotAuthenticated and Severity.Error
3. WHEN HasPrincipal is false on a Protected_Request query returning IQueryResponse\<T\>, THE Authorization_Decorator SHALL return an unsuccessful response with exactly one OutcomeEntry with ErrorCode set to GenericErrorCodes.NotAuthenticated and Severity.Error
4. WHEN HasPrincipal is false on a Protected_Request query returning a Raw_Query_Result, THE Authorization_Decorator SHALL throw a System.UnauthorizedAccessException
5. WHEN HasPrincipal is false on a Protected_Request, THE Authorization_Decorator SHALL NOT invoke the inner handler
6. WHEN HasPrincipal is false on a Protected_Request, THE Authorization_Decorator SHALL NOT evaluate any RBAC clauses (roles or permissions)
7. WHEN HasPrincipal is true but RBAC clauses are not satisfied, THE Authorization_Decorator SHALL use GenericErrorCodes.NotAuthorized (not GenericErrorCodes.NotAuthenticated) in the OutcomeEntry

### Requirement 13: Failure Representation

**User Story:** As a developer, I want authorization failures to produce exactly one outcome entry with no detail leakage, so that the response is consistent and secure.

#### Acceptance Criteria

1. WHEN authorization is denied, THE Authorization_Decorator SHALL append exactly one OutcomeEntry to the response regardless of how many clauses failed
2. THE denied OutcomeEntry SHALL NOT contain a detail message revealing which roles or permissions were missing
3. THE denied OutcomeEntry SHALL NOT contain a list of missing roles or permissions
4. THE denied OutcomeEntry SHALL use Severity.Error

### Requirement 14: Deny By Default Principle

**User Story:** As a developer, I want the system to deny requests when unexpected errors occur during authorization, so that a failure in the authorization pipeline does not accidentally grant access.

#### Acceptance Criteria

1. IF the Authorization_Descriptor resolution throws an exception, THEN THE Authorization_Decorator SHALL deny the request using the same failure representation as a normal denial
2. IF the Authorization_Evaluator throws an unhandled exception during evaluation, THEN THE Authorization_Decorator SHALL deny the request using the same failure representation as a normal denial
3. IF the IAuthorizationContextAccessor throws an exception when retrieving the current context, THEN THE Authorization_Decorator SHALL deny the request using the same failure representation as a normal denial

### Requirement 15: Exception Types for Raw Query Denials

**User Story:** As a developer, I want the decorator to use standard BCL exception types for raw query denials, so that the existing exception decorator can catch them without depending on the authorization extension.

#### Acceptance Criteria

1. WHEN a Raw_Query_Result query is denied due to unsatisfied RBAC clauses (403), THE Authorization_Decorator SHALL throw a System.Security.SecurityException with a generic message that does not reveal specific role or permission details
2. WHEN a Raw_Query_Result query is denied due to HasPrincipal being false (401), THE Authorization_Decorator SHALL throw a System.UnauthorizedAccessException with a generic message that does not reveal internal details
3. THE Authorization_Decorator SHALL NOT define custom exception types for authorization or authentication failures
4. THE thrown exceptions SHALL interact with Minded's existing exception decorator, which can catch and convert them into appropriate error responses

### Requirement 17: Registration API

**User Story:** As a developer, I want fluent extension methods on MindedBuilder to register the authorization decorators with optional configuration, so that setup follows the same pattern as other Minded extensions.

#### Acceptance Criteria

1. THE AddCommandAuthorizationDecorator extension method on MindedBuilder SHALL register the command authorization decorator for ICommand and ICommand\<TResult\> handlers
2. THE AddQueryAuthorizationDecorator extension method on MindedBuilder SHALL register the query authorization decorator for IQuery\<TResult\> handlers
3. THE AddCommandAuthorizationDecorator and AddQueryAuthorizationDecorator methods SHALL return MindedBuilder for fluent chaining
4. THE AddAuthorizationContextAccessor\<TAccessor\> extension method on IServiceCollection SHALL register the provided IAuthorizationContextAccessor implementation
5. THE AddRequestAuthorizationEvaluator\<TEvaluator\> extension method on IServiceCollection SHALL register the provided IRequestAuthorizationEvaluator implementation
6. WHEN AddCommandAuthorizationDecorator or AddQueryAuthorizationDecorator is called, THE registration SHALL eagerly validate all discovered RBAC attributes on registered request types at startup
7. THE AddCommandAuthorizationDecorator method SHALL accept an optional Action\<AuthorizationOptions\> parameter for configuring command authorization behavior
8. THE AddQueryAuthorizationDecorator method SHALL accept an optional Action\<AuthorizationOptions\> parameter for configuring query authorization behavior

### Requirement 18: Decorator Ordering

**User Story:** As a developer, I want the authorization decorator to execute after validation but before logging in the pipeline, so that invalid requests are rejected before authorization and authorized requests are logged.

#### Acceptance Criteria

1. THE command decorator pipeline SHALL execute in the order: validation, transaction, authorization, logging, exception, handler
2. THE query decorator pipeline SHALL execute in the order: validation, cache, authorization, logging, exception, handler

### Requirement 19: REST Mapping Compatibility

**User Story:** As a developer, I want the authorization decorator's error codes to be compatible with the existing DefaultRestRulesProvider, so that denied requests produce correct HTTP responses without any additional configuration or custom providers.

#### Acceptance Criteria

1. THE Authorization_Decorator SHALL use GenericErrorCodes.NotAuthorized for RBAC denials, which the existing DefaultRestRulesProvider already maps to HTTP 403 Forbidden
2. THE Authorization_Decorator SHALL use GenericErrorCodes.NotAuthenticated for unauthenticated denials, which the existing DefaultRestRulesProvider already maps to HTTP 401 Unauthorized
3. THE extension SHALL NOT require a custom IRestRulesProvider implementation; the existing DefaultRestRulesProvider SHALL handle authorization error codes without modification

### Requirement 20: Logging

**User Story:** As a developer, I want the authorization decorator to log high-level authorization outcomes, so that I can monitor access patterns without leaking sensitive details.

#### Acceptance Criteria

1. WHEN authorization is evaluated, THE Authorization_Decorator SHALL log the request type name, the allowed or denied outcome, and the evaluation duration
2. WHEN a request is denied due to HasPrincipal being false, THE Authorization_Decorator SHALL log the outcome as an unauthenticated access attempt, distinct from an unauthorized access attempt
3. WHEN a request is denied due to unsatisfied RBAC clauses, THE Authorization_Decorator SHALL log the outcome as an unauthorized access attempt
4. THE Authorization_Decorator SHALL NOT log which specific roles or permissions were missing
5. THE Authorization_Decorator SHALL NOT log the contents of the Authorization_Context (roles list, permissions list)
6. THE Authorization_Decorator SHALL use ILogger for all logging output

### Requirement 21: Multiple Clause Combination

**User Story:** As a developer, I want multiple RBAC attributes on the same request type to be combined with implicit AND, so that I can express compound authorization rules.

#### Acceptance Criteria

1. WHEN a request type has multiple RequireRolesAttribute instances, THE Authorization_Evaluator SHALL require all role clauses to pass (implicit AND)
2. WHEN a request type has multiple RequirePermissionsAttribute instances, THE Authorization_Evaluator SHALL require all permission clauses to pass (implicit AND)
3. WHEN a request type has both RequireRolesAttribute and RequirePermissionsAttribute instances, THE Authorization_Evaluator SHALL require all role clauses and all permission clauses to pass (implicit AND)

### Requirement 22: Package and Namespace Structure

**User Story:** As a developer, I want the authorization extension to follow Minded's packaging conventions, so that it integrates cleanly with the existing project structure.

#### Acceptance Criteria

1. THE extension SHALL be packaged as Minded.Extensions.Authorization
2. THE public attribute types SHALL reside in the Minded.Extensions.Authorization.Attributes namespace
3. THE decorator types SHALL reside in the Minded.Extensions.Authorization.Decorator namespace
4. THE registration and configuration types SHALL reside in the Minded.Extensions.Authorization.Configuration namespace

### Requirement 23: AllowUnauthenticatedAttribute Definition

**User Story:** As a developer, I want to explicitly mark certain commands or queries as not requiring authentication, so that I can opt specific requests out of the enforce-authentication policy while keeping the rest of the system locked down.

#### Acceptance Criteria

1. THE AllowUnauthenticatedAttribute SHALL target only classes (AttributeTargets.Class)
2. THE AllowUnauthenticatedAttribute SHALL NOT allow multiple instances on the same class (AllowMultiple = false)
3. THE AllowUnauthenticatedAttribute SHALL be inherited by derived classes (Inherited = true)
4. THE AllowUnauthenticatedAttribute SHALL reside in the Minded.Extensions.Authorization.Attributes namespace alongside RequireRolesAttribute and RequirePermissionsAttribute
5. THE AllowUnauthenticatedAttribute SHALL have no constructor parameters or configurable properties
6. WHEN a request type has both AllowUnauthenticatedAttribute and any RBAC attribute (RequireRolesAttribute, RequirePermissionsAttribute, or RequireAuthenticationAttribute), THE Authorization_Decorator registration SHALL throw an InvalidOperationException at startup, because requiring authentication or specific roles/permissions is contradictory with allowing unauthenticated access

### Requirement 24: AuthorizationOptions Configuration

**User Story:** As a developer, I want to configure authorization behavior through an options class following Minded's existing options pattern, so that I can enable enforce-authentication policies and control decorator behavior at registration time.

#### Acceptance Criteria

1. THE AuthorizationOptions class SHALL expose a RequireAuthenticationForAllCommands property of type bool with a default value of false
2. THE AuthorizationOptions class SHALL expose a RequireAuthenticationForAllQueries property of type bool with a default value of false
3. THE AuthorizationOptions class SHALL follow Minded's existing options pattern, supporting both static property values and optional Func\<bool\> provider properties for runtime configuration
4. THE AuthorizationOptions class SHALL expose GetEffectiveRequireAuthenticationForAllCommands() and GetEffectiveRequireAuthenticationForAllQueries() methods that resolve the provider or fall back to the static value

### Requirement 25: Enforce Authentication Policy for Unattributed Requests

**User Story:** As a developer, I want the option to require that all commands or queries carry either an RBAC attribute or an explicit AllowUnauthenticatedAttribute, so that I can enforce a secure-by-default posture where no request slips through without an intentional authorization decision.

#### Acceptance Criteria

1. WHEN RequireAuthenticationForAllCommands is enabled and a command has no RequireRolesAttribute, no RequirePermissionsAttribute, no RequireAuthenticationAttribute, and no AllowUnauthenticatedAttribute, THEN THE Authorization_Decorator SHALL treat the command as a Protected_Request requiring authentication (HasPrincipal must be true) and deny unauthenticated callers with GenericErrorCodes.NotAuthenticated
2. WHEN RequireAuthenticationForAllQueries is enabled and a query has no RequireRolesAttribute, no RequirePermissionsAttribute, no RequireAuthenticationAttribute, and no AllowUnauthenticatedAttribute, THEN THE Authorization_Decorator SHALL treat the query as a Protected_Request requiring authentication (HasPrincipal must be true) and deny unauthenticated callers with GenericErrorCodes.NotAuthenticated
3. WHEN RequireAuthenticationForAllCommands or RequireAuthenticationForAllQueries is enabled and a request carries AllowUnauthenticatedAttribute, THEN THE Authorization_Decorator SHALL pass through to the inner handler without performing any authentication or authorization checks
4. WHEN RequireAuthenticationForAllCommands or RequireAuthenticationForAllQueries is disabled (default), THEN THE Authorization_Decorator SHALL treat unattributed requests as Unprotected_Requests and pass through to the inner handler without performing any checks
5. WHEN RequireAuthenticationForAllCommands or RequireAuthenticationForAllQueries is enabled and a request carries RBAC attributes, THEN THE Authorization_Decorator SHALL evaluate the RBAC attributes normally (authentication-first, then RBAC clauses) as defined in Requirements 10, 11, and 12

### Requirement 26: RequireAuthenticationAttribute Definition

**User Story:** As a developer, I want to place a RequireAuthenticationAttribute on my command or query class, so that I can require an authenticated caller without specifying any particular roles or permissions.

#### Acceptance Criteria

1. THE RequireAuthenticationAttribute SHALL target only classes (AttributeTargets.Class)
2. THE RequireAuthenticationAttribute SHALL NOT allow multiple instances on the same class (AllowMultiple = false)
3. THE RequireAuthenticationAttribute SHALL be inherited by derived classes (Inherited = true)
4. THE RequireAuthenticationAttribute SHALL reside in the Minded.Extensions.Authorization.Attributes namespace alongside RequireRolesAttribute, RequirePermissionsAttribute, and AllowUnauthenticatedAttribute
5. THE RequireAuthenticationAttribute SHALL have no constructor parameters or configurable properties
6. WHEN a request type carries RequireAuthenticationAttribute, THE Authorization_Decorator SHALL check HasPrincipal and deny unauthenticated callers with GenericErrorCodes.NotAuthenticated, following the same failure representation as Requirement 12
7. WHEN a request type carries RequireAuthenticationAttribute and HasPrincipal is true, THE Authorization_Decorator SHALL invoke the inner handler without evaluating any RBAC clauses
8. WHEN a request type carries both RequireAuthenticationAttribute and RBAC attributes (RequireRolesAttribute or RequirePermissionsAttribute), THE Authorization_Decorator SHALL check HasPrincipal first and then evaluate the RBAC clauses normally
