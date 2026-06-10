# Minded Example Application - Quick Start

## Running the Application

### Prerequisites
- .NET 10.0 SDK or later
- SQL Server LocalDB or full instance

### Setup & Debug

1. **Update connection string** in `MindedExample.Api/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MindedExample;Trusted_Connection=True;"
  }
}
```

2. **Run migrations**:
```bash
cd Example/MindedExample.Api
dotnet ef database update --project ../MindedExample.Infrastructure.Persistence
```

3. **Start debugging** (F5 in VS Code):
   - Opens `Full Stack: Frontend + MindedExample.Api` debug config
   - Backend runs on http://localhost:6000
   - Frontend (Vite) runs on http://localhost:3000
   - Database auto-seeds with sample data

4. **View API docs**:
   ```
   http://localhost:6000/swagger
   ```

## Default Admin Login

**Endpoint:** `POST /api/auth/login`

### Seeded Accounts

| Role | Email | Password | Tenant |
|------|-------|----------|--------|
| Global Admin | `admin@example.com` | `Admin1!` | None (cross-tenant) |
| Tenant Admin | `admin-tenant1@example.com` | `Admin1!` | Default Tenant |
| Tenant Admin | `admin-tenant2@example.com` | `Admin1!` | Contoso Demo Tenant |

> Regular users (john.doe, jane.smith, bob.johnson, alice.brown, mark.wilson) have no password set and cannot log in directly.

**Request:**
```bash
POST http://localhost:6000/api/auth/login
Content-Type: application/json

{
  "email": "admin@example.com",
  "password": "Admin1!"
}
```

**Response includes JWT token** for authenticated requests.

## Logs

In development, logs are written to:
```
Example/MindedExample.Api/Logs/api-dev-<date>.txt
```
Configured via `LogFile` in `appsettings.Development.json`. The log level defaults to `Debug` in development.

## Sample Data

Automatically seeded in development:

- **Users:** Admin (admin@example.com), John Doe, Jane Smith, Bob Johnson
- **Categories:** Groceries, Utilities, Transportation, Entertainment, Healthcare, etc.
- **Transactions:** 15 sample transactions across categories and users

## Key Features

- **CQRS Pattern** - Commands & Queries with dedicated handlers
- **OData Support** - `$filter`, `$orderby`, `$top/$skip`, `$expand`, `$select`
- **Validation** - FluentValidation on all commands
- **JWT Authentication** - Role-based authorization (Admin, User)
- **Real-time Logging** - SignalR-based log streaming to frontend
- **Swagger UI** - Interactive API documentation

## API Quick Examples

```bash
# Get all categories
GET /api/category

# Get with OData filter & sort
GET /api/category?$filter=name eq 'Groceries'&$orderby=name desc

# Expand navigation properties
GET /api/category?$expand=Transactions

# Create category
POST /api/category
{
  "name": "Electronics",
  "description": "Electronic items"
}

# Get configuration
GET /api/configurations/System.MinimumLogLevel

# Update logging level (Admin only)
PUT /api/configurations/System.MinimumLogLevel
{
  "value": "Debug"
}
```

## Project Structure

```
MindedExample.Api/              # REST entry point
MindedExample.Domain/           # Entities (User, Category, Transaction, etc.)
MindedExample.Infrastructure.*/  # Data access & configuration
MindedExample.Application.*     # CQRS handlers (Category, Transaction, User, Role, Configuration)
Tests/                          # Unit & integration tests
Frontend/                       # React + Vite UI (optional)
```

## More Information

- **Framework:** [../README.md](../README.md)
- **Database Seeding:** [MindedExample.Infrastructure.Persistence/README_DatabaseSeeding.md](MindedExample.Infrastructure.Persistence/README_DatabaseSeeding.md)
- **OData Details:** [MindedExample.Api/OData/README.md](MindedExample.Api/OData/README.md)
