# Command and Query Catalogue

This catalogue documents CQRS requests in the Example application and the cross-cutting attributes/security constraints currently applied.

| Type | Name | Brief Description | Attributes / Cross-Cutting | Security Constraints |
|---|---|---|---|---|
| Command | CreateCategoryCommand | Create a tenant category. | ValidateCommand, RetryCommand, RequirePermissions(CanCreateCategory), RequireClaim(tenant_id), RequireClaim(is_global_admin=false), Logging | Permission CanCreateCategory, tenant_id required, global admin denied |
| Command | UpdateCategoryCommand | Update a tenant category by id. | ValidateCommand, RequirePermissions(CanUpdateCategory), RequireClaim(tenant_id), RequireClaim(is_global_admin=false), Logging | Permission CanUpdateCategory, tenant_id required, global admin denied |
| Command | DeleteCategoryCommand | Delete a tenant category by id. | ValidateCommand, RequirePermissions(CanDeleteCategory), RequireClaim(tenant_id), RequireClaim(is_global_admin=false), Logging | Permission CanDeleteCategory, tenant_id required, global admin denied |
| Query | GetCategoriesQuery | List categories with OData options. | ValidateQuery, RequireClaim(tenant_id), RequireClaim(is_global_admin=false), Logging | tenant_id required, global admin denied |
| Query | GetCategoryByIdQuery | Get category details by id. | MemoryCache(ExpirationInSeconds=300), RequireClaim(tenant_id), RequireClaim(is_global_admin=false), IGenerateCacheKey, Logging | tenant_id required, global admin denied |
| Query | ExistsCategoryByIdQuery | Check category existence by id. | Logging | No explicit attribute-level authorization |
| Command | CreateTransactionCommand | Create a transaction. | ValidateCommand, RequirePermissions(CanCreateTransaction), RequireClaim(tenant_id), RequireClaim(is_global_admin=false), Logging | Permission CanCreateTransaction, tenant_id required, global admin denied |
| Command | UpdateTransactionCommand | Update a transaction by id. | ValidateCommand, RequirePermissions(CanUpdateTransaction), RequireClaim(tenant_id), RequireClaim(is_global_admin=false), Logging | Permission CanUpdateTransaction, tenant_id required, global admin denied |
| Command | DeleteTransactionCommand | Delete a transaction by id. | ValidateCommand, RequirePermissions(CanDeleteTransaction), RequireClaim(tenant_id), RequireClaim(is_global_admin=false), Logging | Permission CanDeleteTransaction, tenant_id required, global admin denied |
| Query | GetTransactionsQuery | List transactions with OData options. | RequireClaim(tenant_id), RequireClaim(is_global_admin=false), Logging | tenant_id required, global admin denied |
| Query | GetTransactionByIdQuery | Get transaction details by id. | RequireClaim(tenant_id), RequireClaim(is_global_admin=false), Logging | tenant_id required, global admin denied |
| Query | ExistsTransactionByIdQuery | Check transaction existence by id. | Logging | No explicit attribute-level authorization |
| Command | CreateRoleCommand | Create role definition in tenant scope. | ValidateCommand, RequirePermissions(CanCreateRole), Logging | Permission CanCreateRole |
| Command | UpdateRolePermissionsCommand | Update permissions assigned to a role. | ValidateCommand, RequirePermissions(CanUpdateRolePermissions), Logging | Permission CanUpdateRolePermissions |
| Command | DeleteRoleCommand | Delete a role. | ValidateCommand, RequirePermissions(CanDeleteRole), Logging | Permission CanDeleteRole |
| Command | AssignRolesToUserCommand | Assign role set to a tenant user. | ValidateCommand, RequirePermissions(CanAssignRoles), Logging | Permission CanAssignRoles |
| Command | ResetRolesToDefaultCommand | Reset roles/permissions to defaults. | RequirePermissions(CanManageRoles), Logging | Permission CanManageRoles |
| Query | GetRolesQuery | List roles and metadata. | Logging | No explicit attribute-level authorization |
| Query | GetPermissionsQuery | List role-permission matrix. | Logging | No explicit attribute-level authorization |
| Query | GetUsersWithRolesQuery | List users enriched with assigned roles. | Logging | No explicit attribute-level authorization |
| Command | UpdateConfigurationCommand | Update runtime configuration value by key. | ValidateCommand, RequirePermissions(CanUpdateConfiguration), Logging | Permission CanUpdateConfiguration |
| Command | CreateTenantCommand | Create a new tenant and legal owner. | ValidateCommand, Logging | Exposed through AdminTenantsController policy GlobalAdminOnly; handler checks IsGlobalAdmin |
| Command | DeleteTenantCommand | Delete tenant and tenant-scoped data. | ValidateCommand, Logging | Exposed through AdminTenantsController policy GlobalAdminOnly; handler checks IsGlobalAdmin |
| Query | GetAllConfigurationsQuery | List runtime configuration entries. | Logging | No explicit attribute-level authorization |
| Query | GetConfigurationByKeyQuery | Retrieve one runtime configuration by key. | ValidateQuery, Logging | No explicit attribute-level authorization |
| Query | GetAdminTenantSummariesQuery | List tenant summaries for global admin operations. | Logging | Exposed through AdminTenantsController policy GlobalAdminOnly; handler checks IsGlobalAdmin |
| Command | CreateUserCommand | Create user account. | ValidateCommand, Logging | No explicit attribute-level authorization |
| Command | UpdateUserCommand | Update user profile data. | ValidateCommand, Logging | No explicit attribute-level authorization |
| Command | DeleteUserCommand | Delete a user. | ValidateCommand, Logging | No explicit attribute-level authorization |
| Query | GetUsersQuery | List users with OData options. | ValidateQuery, Logging | No explicit attribute-level authorization |
| Query | GetUserByIdQuery | Retrieve user by id. | MemoryCache(60), Logging | No explicit attribute-level authorization |

## Notes

- Attribute-level constraints are enforced by Minded authorization decorators when command/query authorization decorators are registered.
- API-level policies remain applicable where controllers use ASP.NET Authorize attributes.
- Claims-based tenant scope now uses tenant_id presence and is_global_admin=false constraints on category and transaction request models.
