# MindedExample Database - ER Diagram

```mermaid
erDiagram
    Users {
        int Id PK
        varchar100 Name "Required"
        varchar100 Surname "Required"
        varchar250 Email "Required"
    }

    Roles {
        int Id PK
        varchar100 Name "Required, Unique"
        varchar500 Description
    }

    Permissions {
        int Id PK
        varchar100 Name "Required, Unique"
        varchar500 Description
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
        int UserId FK
        int RoleId FK
    }

    RolePermissions {
        int RoleId FK
        int PermissionId FK
    }

    %% Roles & Permissions (many-to-many)
    Users ||--o{ UserRoles : "has"
    Roles ||--o{ UserRoles : "assigned to"
    Roles ||--o{ RolePermissions : "grants"
    Permissions ||--o{ RolePermissions : "belongs to"

    %% User relationships
    Users ||--o{ Categories : "owns"
    Users ||--o{ Transactions : "records"

    %% Category hierarchy (self-referencing)
    Categories ||--o{ Categories : "parent of"

    %% Category-Transaction
    Categories ||--o{ Transactions : "contains"
```

## Seeded Roles & Permissions

### Roles
| Role  | Description |
|-------|-------------|
| Admin | Full system access including user and role management |
| User  | Standard user with transaction management permissions |

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
