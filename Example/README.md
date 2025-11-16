# Minded Example Application

This is a complete example application demonstrating how to use the Minded framework to build a real-world REST API with CQRS, OData support, validation, and Swagger documentation.

## Overview

The example implements a simple transaction management system with:
- **Categories** - Organize transactions into categories
- **Transactions** - Financial transactions with amounts and descriptions
- **Users** - User accounts associated with transactions

## Architecture

The example follows the Minded framework's recommended structure:

```
Example/
├── Application.Api/          # REST API entry point
│   ├── Controllers/          # REST controllers using RestMediator
│   ├── OData/               # OData navigation property serialization
│   └── Startup.cs           # Application configuration
├── Service.Category/         # Category business logic
│   ├── Command/             # Commands (Create, Update, Delete)
│   ├── CommandHandler/      # Command handlers
│   ├── Query/               # Queries (Get, List)
│   ├── QueryHandler/        # Query handlers
│   └── Validator/           # FluentValidation validators
├── Service.Transaction/      # Transaction business logic
├── Service.Common/          # Shared service utilities
├── Data.Entity/             # Entity models
├── Data.Context/            # EF Core DbContext
└── Tests/                   # Unit and integration tests
```

## Features Demonstrated

### ✅ CQRS Pattern
- Commands for state changes (Create, Update, Delete)
- Queries for data retrieval (Get, List)
- Handlers for each operation
- RestMediator for HTTP response mapping

### ✅ OData Support
- `$filter` - Filter results
- `$orderby` - Sort results
- `$top` / `$skip` - Pagination
- `$count` - Get total count
- `$expand` - Load navigation properties
- `$select` - Choose specific fields

### ✅ Validation
- FluentValidation integration
- Automatic validation before command execution
- Validation error responses

### ✅ Swagger/OpenAPI
- Auto-generated API documentation
- Interactive API testing
- OData query parameter support
- XML documentation comments

### ✅ Entity Framework Core
- Code-first migrations
- Database seeding
- Navigation properties
- Async operations

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- SQL Server (LocalDB or full instance)

### Running the Application

1. **Update the connection string** in `Application.Api/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MindedExample;Trusted_Connection=True;"
  }
}
```

2. **Run database migrations**:
```bash
cd Example/Application.Api
dotnet ef database update --project ../Data.Context
```

3. **Run the application**:
```bash
dotnet run --project Application.Api
```

4. **Open Swagger UI**:
```
https://localhost:5001/swagger
```

## API Examples

### Get All Categories
```bash
GET /api/category
```

### Get Category by ID
```bash
GET /api/category/1
```

### Get Categories with Filtering and Sorting
```bash
GET /api/category?$filter=name eq 'Electronics'&$orderby=name desc
```

### Get Categories with Transactions (Expanded)
```bash
GET /api/category?$expand=Transactions
```

### Create a Category
```bash
POST /api/category
Content-Type: application/json

{
  "name": "Electronics",
  "description": "Electronic items"
}
```

### Update a Category
```bash
PUT /api/category/1
Content-Type: application/json

{
  "id": 1,
  "name": "Electronics Updated",
  "description": "Updated description"
}
```

### Delete a Category
```bash
DELETE /api/category/1
```

## OData Navigation Property Serialization

By default, Entity Framework navigation properties are serialized in JSON responses, which can cause:
- **Circular reference errors** when entities reference each other
- **Performance issues** from loading and serializing unwanted data
- **Security concerns** by exposing more data than intended

This example demonstrates automatic handling of navigation properties through OData's `$expand` parameter.

**Setup in Startup.cs**:
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddOData();

    services.AddMvc(options => options.EnableEndpointRouting = false)
        .AddODataNavigationPropertySerialization();  // ← Add this line
}
```

**Without $expand** (navigation properties excluded):
```bash
GET /api/category/1
```
```json
{
  "id": 1,
  "name": "Electronics",
  "description": "Electronic items"
}
```

**With $expand** (navigation properties included):
```bash
GET /api/category?$expand=Transactions
```
```json
{
  "id": 1,
  "name": "Electronics",
  "transactions": [
    { "id": 1, "description": "Purchase", "amount": 99.99 }
  ]
}
```

This is handled automatically by:
1. **`ODataExpandActionFilter`** - Captures `$expand` parameters from requests
2. **`IgnoreNavigationPropertiesResolver`** - Controls JSON serialization based on `$expand`

**See**: [OData Navigation Property Serialization Documentation](Application.Api/OData/README.md) for detailed implementation.

## Testing

The example includes comprehensive tests:

### Unit Tests
- **Service.Category.Tests** - Category service unit tests
- **Service.Transaction.Tests** - Transaction service unit tests
- **Application.Api.Tests** - API controller tests

### Integration Tests
- **Service.Category.IntegrationTests** - Category service with database
- **Service.Transaction.IntegrationTests** - Transaction service with database
- **Common.Integration.Tests** - Shared integration test utilities

### E2E Tests
- **Application.Api.E2ETests** - Full API end-to-end tests

Run all tests:
```bash
cd Example
dotnet test
```

## Key Patterns Demonstrated

### RestMediator Pattern
Controllers use `RestMediator` to process commands and queries with automatic HTTP response mapping:

```csharp
[HttpGet]
public async Task<IActionResult> Get([FromQuery] ODataQueryOptions<Category> queryOptions)
{
    var query = new GetCategoriesQuery();
    query.ApplyODataQueryOptions(queryOptions);
    return await _restMediator.ProcessRestQueryAsync(RestOperation.GetMany, query);
}
```

### Command/Query Handlers
Each operation has a dedicated handler:

```csharp
public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, Category>
{
    private readonly IMindedExampleContext _context;

    public async Task<Category> HandleAsync(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Name = command.Name,
            Description = command.Description
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return category;
    }
}
```

### Validation
FluentValidation automatically validates commands:

```csharp
public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required")
            .MaximumLength(100).WithMessage("Category name must not exceed 100 characters");
    }
}
```

## Learn More

- **Minded Framework Documentation**: [../README.md](../README.md)
- **OData Implementation Details**: [Application.Api/OData/README.md](Application.Api/OData/README.md)
- **Database Seeding**: [Data.Context/README_DatabaseSeeding.md](Data.Context/README_DatabaseSeeding.md)

## License

This example application is part of the Minded framework and is licensed under the MIT License.

