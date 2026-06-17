# Command and Query Catalogue

This catalogue documents CQRS requests in the Example application, the decorator attributes applied, and the primary framework feature each entry is designed to showcase.

## Decorator Legend

| Decorator / Feature | Description |
|---|---|
| `[ValidateCommand]` / `[ValidateQuery]` | Triggers the validation decorator pipeline; runs the matching `ICommandValidator<T>` or `IQueryValidator<T>` before the handler. |
| `[RetryCommand]` / `[RetryQuery]` | Retries the handler on failure; count and delays configured via attribute arguments or `RetryOptions`. |
| `[TransactionalCommand]` | Wraps the handler in a `TransactionScope`; all SaveChangesAsync calls inside the handler (including nested mediator dispatches) participate in the same transaction. |
| `[MemoryCache]` + `IGenerateCacheKey` | Caches query results in memory; `IGenerateCacheKey` provides an explicit, deterministic cache key. |
| `[MemoryCache]` (auto key) | Caches query results; cache key is auto-generated from the query type and property values when `IGenerateCacheKey` is not implemented. |
| `[RequirePermissions]` | Enforces permission-based authorization via the authorization decorator; evaluated against the current user's claims. Supports `Match = AuthorizationMatch.All / Any / AtLeast / None`. |
| `[RequireRoles]` | Enforces role-based authorization via the authorization decorator; the caller must hold the specified roles. Supports the same `Match` modes as `[RequirePermissions]`, plus `OrAnyRole` / `OrAnyPermission` escape-hatch properties. |
| `[RequireClaim]` | Enforces claim-based authorization via the authorization decorator (e.g. `is_global_admin=false` to restrict tenant operations). Supports `MatchProperty` for dynamic comparison against a request property value. |
| `[RequireAuthentication]` | Requires an authenticated caller (principal must be present) without imposing any specific role or permission; useful when only identity is needed. |
| `[AllowUnauthenticated]` | Explicitly opts a command or query out of the global enforce-authentication policy; allows anonymous access even when `RequireAuthenticationForAllCommands / Queries` is `true`. |
| `[RequireResourceAccess]` | Delegates resource-level authorization to a dedicated `IQuery` that receives the resource id (from a request property) and a caller identifier (from a claim); the query result determines allow/deny. |
| `ILoggable` | Enables structured logging through the logging decorator; `LoggingTemplate` and `LoggingProperties` control what is logged. |
| `[SensitiveData]` | Marks domain entity properties (e.g. `User.Email`, `Category.Name`) as sensitive; the DataProtection sanitizer redacts them from logs and exception output. |
| Exception decorator (global) | Registered globally; catches unhandled exceptions from any handler and returns a structured error response. |
| Context decorator (global) | Registered globally; populates `IMindedContext` (TraceId, user info) for the duration of every command/query execution. |

---

## Category Domain

| Type | Name | Brief Description | Attributes / Cross-Cutting | Security Constraints | Feature Showcased |
|---|---|---|---|---|---|
| Command | CreateCategoryCommand | Create a tenant category. | `[ValidateCommand]`, `[RetryCommand]`, `[RequirePermissions(CanCreateCategory)]`, `[RequireClaim(is_global_admin=false)]`, `ILoggable` | Permission CanCreateCategory; global admin denied | **`[ValidateCommand]` + `[RetryCommand]` + `[RequirePermissions]`** — canonical example of a write command with validation, idempotent retry, and permission guard |
| Command | UpdateCategoryCommand | Update a tenant category by id. | `[ValidateCommand]`, `[RetryCommand]`, `[RequirePermissions(CanUpdateCategory)]`, `[RequireClaim(is_global_admin=false)]`, `ILoggable` | Permission CanUpdateCategory; global admin denied | **`[RetryCommand]`** — demonstrates that update commands are safe to retry on transient failures |
| Command | DeleteCategoryCommand | Delete a tenant category by id. | `[ValidateCommand]`, `[RetryCommand]`, `[RequirePermissions(CanDeleteCategory)]`, `[RequireClaim(is_global_admin=false)]`, `ILoggable` | Permission CanDeleteCategory; global admin denied | **`[RetryCommand]`** — demonstrates retry on idempotent delete |
| Query | GetCategoriesQuery | List categories with OData options. | `[ValidateQuery]`, `[RequireClaim(is_global_admin=false)]`, OData traits (`ICanCount`, `ICanTop`, `ICanSkip`, `ICanExpand`, `ICanOrderBy`, `ICanFilterExpression`), `ILoggable` | Global admin denied | **`[ValidateQuery]` + OData traits** — full OData-capable query with validator |
| Query | GetCategoryByIdQuery | Get category details by id. | `[MemoryCache(ExpirationInSeconds=300)]`, `IGenerateCacheKey`, `[RequireClaim(is_global_admin=false)]`, `ILoggable` | Global admin denied | **`[MemoryCache]` + `IGenerateCacheKey`** — caching with an explicit, deterministic cache key |
| Query | ExistsCategoryInCurrentTenantQuery | Check category existence by id within the current caller's tenant. | `ILoggable` | Tenant-scoped by handler | **Minimal query** — no decorators beyond logging; used internally by validators |

---

## Transaction Domain

| Type | Name | Brief Description | Attributes / Cross-Cutting | Security Constraints | Feature Showcased |
|---|---|---|---|---|---|
| Command | CreateTransactionCommand | Create a transaction. | `[ValidateCommand]`, `[RequirePermissions(CanCreateTransaction)]`, `[RequireClaim(is_global_admin=false)]`, `ILoggable` | Permission CanCreateTransaction; global admin denied | **`[ValidateCommand]` + `[RequirePermissions]` + `[RequireClaim]`** — command with validation, permission and claim guards |
| Command | UpdateTransactionCommand | Update a transaction by id. | `[ValidateCommand]`, `[RequirePermissions(CanUpdateTransaction)]`, `[RequireClaim(is_global_admin=false)]`, `ILoggable` | Permission CanUpdateTransaction; global admin denied | **`[ValidateCommand]` + `[RequirePermissions]`** |
| Command | DeleteTransactionCommand | Delete a transaction by id. | `[ValidateCommand]`, `[RequirePermissions(CanDeleteTransaction)]`, `[RequireClaim(is_global_admin=false)]`, `ILoggable` | Permission CanDeleteTransaction; global admin denied | **`[ValidateCommand]` + `[RequirePermissions]`** |
| Query | GetTransactionsQuery | List transactions with OData options. | `[ValidateQuery]`, `[RequireClaim(is_global_admin=false)]`, OData traits (`ICanCount`, `ICanTop`, `ICanSkip`, `ICanExpand`, `ICanOrderBy`, `ICanFilterExpression`), `ILoggable` | Global admin denied | **`[ValidateQuery]` + OData traits** |
| Query | GetTransactionByIdQuery | Get transaction details by id. | `[RetryQuery]`, `[RequireClaim(is_global_admin=false)]`, `ILoggable` | Global admin denied | **`[RetryQuery]`** — only query in the catalogue that showcases query-level retry for transient read failures |
| Query | ExistsTransactionByIdQuery | Check transaction existence by id. | `ILoggable` | None | **Minimal query** — used internally by validators |

---

## Role Domain

| Type | Name | Brief Description | Attributes / Cross-Cutting | Security Constraints | Feature Showcased |
|---|---|---|---|---|---|
| Command | CreateRoleCommand | Create a role definition in tenant scope. | `[ValidateCommand]`, `[RequirePermissions(CanCreateRole)]`, `ILoggable` | Permission CanCreateRole | **`[ValidateCommand]` + `[RequirePermissions]`** |
| Command | UpdateRolePermissionsCommand | Replace all permissions assigned to a role. | `[ValidateCommand]`, `[RequirePermissions(CanUpdateRolePermissions)]`, `ILoggable` | Permission CanUpdateRolePermissions | **`[ValidateCommand]` + `[RequirePermissions]`** |
| Command | DeleteRoleCommand | Delete a role. | `[ValidateCommand]`, `[RequirePermissions(CanDeleteRole)]`, `ILoggable` | Permission CanDeleteRole | **`[ValidateCommand]` + `[RequirePermissions]`** |
| Command | AssignRolesToUserCommand | Assign a set of roles to a tenant user. | `[ValidateCommand]`, `[RequirePermissions(CanAssignRoles)]`, `ILoggable` | Permission CanAssignRoles | **`[ValidateCommand]` + `[RequirePermissions]`** |
| Command | ResetRolesToDefaultCommand | Reset roles/permissions to defaults. | `[RequirePermissions(CanManageRoles)]`, `ILoggable` | Permission CanManageRoles | **`[RequirePermissions]` without `[ValidateCommand]`** — shows that validation is opt-in |
| Query | GetRolesQuery | List roles and metadata. | `ILoggable` | None | **`ILoggable` only** — minimal query with no authorization constraints |
| Query | GetPermissionsQuery | List the full permission set grouped by category. | `ILoggable` | None | **`ILoggable` only** |
| Query | GetUsersWithRolesQuery | List users with their assigned roles. | `ILoggable` | None | **`ILoggable` only** |

---

## Configuration Domain

| Type | Name | Brief Description | Attributes / Cross-Cutting | Security Constraints | Feature Showcased |
|---|---|---|---|---|---|
| Command | UpdateConfigurationCommand | Update a runtime configuration value by key. | `[ValidateCommand]`, `[RequirePermissions(CanUpdateConfiguration)]`, `ILoggable` | Permission CanUpdateConfiguration | **`[ValidateCommand]` + `[RequirePermissions]`** |
| Command | CreateTenantCommand | Create a new tenant and its legal owner user. | `[ValidateCommand]`, `[TransactionalCommand]`, `[RequireClaim(is_global_admin=true)]`, `ILoggable` | Global admin only | **`[TransactionalCommand]`** — handler creates Tenant, User, role-permission rows and user-role rows across multiple `SaveChangesAsync` calls; `[TransactionalCommand]` ensures all succeed or all roll back |
| Command | DeleteTenantCommand | Delete a tenant and all tenant-scoped data. | `[ValidateCommand]`, `[RequireClaim(is_global_admin=true)]`, `ILoggable` | Global admin only | **`[RequireClaim(is_global_admin=true)]`** — shows claim guard for global-admin-only destructive operations |
| Query | GetAllConfigurationsQuery | List all runtime configuration entries. | `ILoggable` | None | **`ILoggable` only** |
| Query | GetConfigurationByKeyQuery | Retrieve one runtime configuration entry by key. | `ILoggable` | None | **No `[ValidateQuery]`** — query returns `IQuery<T>` (non-envelope shape) so the validation decorator cannot be applied; documented intentionally |
| Query | GetAdminTenantSummariesQuery | List tenant summaries for global admin operations. | `[RequireClaim(is_global_admin=true)]`, `ILoggable` | Global admin only | **`[RequireClaim(is_global_admin=true)]`** on a query — shows claim-based authorization on the read side |

---

## User Domain

| Type | Name | Brief Description | Attributes / Cross-Cutting | Security Constraints | Feature Showcased |
|---|---|---|---|---|---|
| Command | CreateUserCommand | Create a user account. | `[ValidateCommand]`, `ILoggable` | None | **`[SensitiveData]`** — `User.Name`, `User.Surname`, `User.Email`, `User.PasswordHash` are marked `[SensitiveData]`; the DataProtection sanitizer redacts them from logs automatically |
| Command | UpdateUserCommand | Update user profile data. | `[ValidateCommand]`, `ILoggable` | None | **`[ValidateCommand]`** with `[SensitiveData]` protection via domain entity |
| Command | DeleteUserCommand | Delete a user. | `[ValidateCommand]`, `ILoggable` | None | **`[ValidateCommand]`** |
| Query | GetUsersQuery | List users with OData options. | `[ValidateQuery]`, OData traits (`ICanCount`, `ICanTop`, `ICanSkip`, `ICanExpand`, `ICanOrderBy`, `ICanFilterExpression`), `ILoggable` | None | **`[SensitiveData]`** on `User` entity combined with full OData support |
| Query | GetUserByIdQuery | Retrieve user by id. | `[MemoryCache(60)]`, `ILoggable` | None | **`[MemoryCache]` without `IGenerateCacheKey`** — cache key is auto-generated from query type and property values (contrast with `GetCategoryByIdQuery` which uses an explicit key) |

---

## Notes

- **Exception decorator** and **Context decorator** are registered globally in `Startup.cs` — every command and query benefits from them without any per-class attribute.
- **DataProtection** is registered globally (`builder.AddDataProtection()`). Domain properties marked `[SensitiveData]` are automatically redacted in logs and exception output. The `ShowSensitiveData` flag is runtime-configurable via `UpdateConfigurationCommand`.
- **Decorator registration order** in `Startup.cs` (commands): `Validation` → `Transaction` → `Exception` → `Retry` → `Authorization` → `Logging`. Execution order is reversed: Logging → Authorization → Retry → Exception → Transaction → Validation → Handler.
- `[ValidateQuery]` requires the query to return `IQuery<IQueryResponse<T>>`; queries returning a plain `IQuery<T>` (e.g. `GetConfigurationByKeyQuery`) cannot use it.
- `[RetryQuery]` is opt-in per query; the query retry decorator is registered with `applyToAllQueries: false`.
- Attribute-level authorization (`[RequirePermissions]`, `[RequireClaim]`) is enforced by the authorization decorator registered in the pipeline; API-level `[Authorize]` policies on controllers provide an independent outer guard.
