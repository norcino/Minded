# MindedExample Database - ER Diagram

```mermaid
erDiagram
    Users {
        int Id PK
        varchar100 Name "Required"
        varchar100 Surname "Required"
        varchar250 Email "Required"
        int TenantId FK "Required"
        varchar20 TenantRole "Required, default Member"
        varchar500 PasswordHash "Nullable"
        bit IsActive "Required, default true"
        %% Indexes: UX Email, IX(TenantId, Email)
    }

    Tenants {
        int Id PK
        varchar200 Name "Required"
    }

    TenantInvites {
        int Id PK
        int TenantId FK "Required"
        int CreatedByUserId FK "Required"
        varchar250 Email "Nullable"
        varchar32 Code "Required, Unique"
        varchar200 Token "Required, Unique"
        datetime ExpiresAtUtc "Required"
        datetime CreatedAtUtc "Required"
        datetime UsedAtUtc "Nullable"
        int UsedByUserId FK "Nullable"
    }

    PasswordResetTokens {
        int Id PK
        int UserId FK "Required"
        varchar200 Token "Required, Unique"
        datetime ExpiresAtUtc "Required"
        datetime CreatedAtUtc "Required"
        datetime UsedAtUtc "Nullable"
    }

    Categories {
        int Id PK
        varchar250 Name "Required"
        varchar500 Description "Required"
        bit Active
        int UserId FK "Required"
        int ParentId FK "Nullable (self-ref)"
    }

    Transactions {
        int Id PK
        datetime Recorded
        money Credit
        money Debit
        varchar500 Description
        int CategoryId FK "Default: 0"
        int UserId FK
    }

    UserRoles {
        int TenantId FK "PK"
        int UserId FK "PK"
        varchar100 RoleName "PK"
    }

    RolePermissions {
        int TenantId FK "PK"
        varchar100 RoleName "PK"
        varchar100 PermissionName "PK"
    }

    %% Tenancy
    Tenants ||--o{ Users : "contains"
    Tenants ||--o{ UserRoles : "scopes"
    Tenants ||--o{ RolePermissions : "scopes"
    Tenants ||--o{ TenantInvites : "owns"

    %% User relationships
    Users ||--o{ Categories : "owns"
    Users ||--o{ Transactions : "records"
    Users ||--o{ PasswordResetTokens : "has"
    Users ||--o{ TenantInvites : "creates"
    Users ||--o| TenantInvites : "redeems (optional)"
    Users ||--o{ UserRoles : "has roles"

    %% Category hierarchy (self-referencing)
    Categories ||--o{ Categories : "parent of"

    %% Category-Transaction
    Categories ||--o{ Transactions : "contains"
```

## Notes

- `Roles` and `Permissions` are no longer persisted as standalone tables.
- Role assignments are stored in `UserRoles` (`TenantId`, `UserId`, `RoleName`).
- Role-to-permission mappings are stored in `RolePermissions` (`TenantId`, `RoleName`, `PermissionName`).

## Seeded Roles & Permissions (Logical)

### Roles
| Role  | Description |
|-------|-------------|
| Admin | Full tenant-level access including member and role management |
| User  | Standard tenant member permissions |

### Permission Matrix
| Permission            | Admin | User |
|-----------------------|:-----:|:----:|
| CanCreateCategory     |   ✅  |  ❌  |
| CanCreateRootCategory |   ✅  |  ❌  |
| CanUpdateCategory     |   ✅  |  ❌  |
| CanDeleteCategory     |   ✅  |  ❌  |
| CanCreateTransaction  |   ✅  |  ✅  |
| CanUpdateTransaction  |   ✅  |  ✅  |
| CanDeleteTransaction  |   ✅  |  ✅  |
| CanCreateUser         |   ✅  |  ❌  |
| CanUpdateUser         |   ✅  |  ❌  |
| CanDeleteUser         |   ✅  |  ❌  |
| CanManageRoles        |   ✅  |  ❌  |
| CanAssignRoles        |   ✅  |  ❌  |
