# Minded.Extensions.OData

Core OData utilities and extensions for Minded Framework, providing foundational OData support for querying, filtering, sorting, and pagination.

## Features

- **OData Query Parsing** - Parse OData query strings into LINQ expressions
- **OData Expression Builders** - Build OData-compatible queries
- **ASP.NET Core OData Integration** - Seamless integration with ASP.NET Core OData
- **Query Composition** - Compose complex queries with $filter, $orderby, $top, $skip
- **Entity Framework Core Support** - Works with EF Core IQueryable
- **Type-Safe Queries** - Strongly-typed query building

## Installation

```bash
dotnet add package Minded.Extensions.OData
dotnet add package Microsoft.AspNetCore.OData
```

## Quick Start

### 1. Configure OData in ASP.NET Core

```csharp
using Microsoft.AspNetCore.OData;
using Microsoft.OData.ModelBuilder;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var modelBuilder = new ODataConventionModelBuilder();
        modelBuilder.EntitySet<Product>("Products");
        modelBuilder.EntitySet<Order>("Orders");
        modelBuilder.EntitySet<Customer>("Customers");

        services.AddControllers()
            .AddOData(options => options
                .Select()
                .Filter()
                .OrderBy()
                .Expand()
                .Count()
                .SetMaxTop(100)
                .AddRouteComponents("odata", modelBuilder.GetEdmModel()));

        services.AddMinded(builder =>
        {
            builder.AddQueryLoggingDecorator();
            builder.AddQueryExceptionDecorator();
        });
    }
}
```

### 2. Create an OData Query

```csharp
using Minded.Framework.CQRS.Abstractions;
using Microsoft.AspNetCore.OData.Query;

public class GetProductsODataQuery : IQuery<IQueryable<Product>>
{
    public ODataQueryOptions<Product> QueryOptions { get; set; }
}

public class GetProductsODataQueryHandler : IQueryHandler<GetProductsODataQuery, IQueryable<Product>>
{
    private readonly ApplicationDbContext _context;

    public GetProductsODataQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<QueryResponse<IQueryable<Product>>> HandleAsync(
        GetProductsODataQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<Product> products = _context.Products;

        // Apply OData query options
        if (query.QueryOptions != null)
        {
            products = (IQueryable<Product>)query.QueryOptions.ApplyTo(products);
        }

        return QueryResponse<IQueryable<Product>>.Success(products);
    }
}
```

### 3. Create an OData Controller

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Minded.Extensions.WebApi;

[Route("odata/[controller]")]
public class ProductsController : ODataController
{
    private readonly IRestMediator _mediator;

    public ProductsController(IRestMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [EnableQuery(MaxTop = 100, AllowedQueryOptions = AllowedQueryOptions.All)]
    public async Task<IActionResult> Get(ODataQueryOptions<Product> queryOptions)
    {
        var query = new GetProductsODataQuery { QueryOptions = queryOptions };
        var result = await _mediator.ProcessQueryAsync(query);

        if (result is OkObjectResult okResult && okResult.Value is IQueryable<Product> products)
        {
            return Ok(products);
        }

        return result;
    }

    [HttpGet("{key}")]
    [EnableQuery]
    public async Task<IActionResult> Get(int key)
    {
        var query = new GetProductByIdQuery { ProductId = key };
        return await _mediator.ProcessQueryAsync(query);
    }
}
```

## OData Query Examples

### Filtering ($filter)

```http
GET /odata/Products?$filter=Price gt 100
GET /odata/Products?$filter=Category eq 'Electronics'
GET /odata/Products?$filter=Price gt 100 and Price lt 500
GET /odata/Products?$filter=contains(Name, 'Phone')
GET /odata/Products?$filter=startswith(Name, 'iPhone')
GET /odata/Products?$filter=InStock eq true
```

### Sorting ($orderby)

```http
GET /odata/Products?$orderby=Price
GET /odata/Products?$orderby=Price desc
GET /odata/Products?$orderby=Category, Price desc
GET /odata/Products?$orderby=Name asc
```

### Pagination ($top, $skip)

```http
GET /odata/Products?$top=10
GET /odata/Products?$skip=20&$top=10
GET /odata/Products?$skip=0&$top=20&$orderby=Name
```

### Selecting Fields ($select)

```http
GET /odata/Products?$select=Id,Name,Price
GET /odata/Products?$select=Name,Category
```

### Expanding Related Data ($expand)

```http
GET /odata/Orders?$expand=Customer
GET /odata/Orders?$expand=OrderItems($expand=Product)
GET /odata/Orders?$expand=Customer,OrderItems
```

### Counting ($count)

```http
GET /odata/Products/$count
GET /odata/Products?$count=true
GET /odata/Products?$filter=Price gt 100&$count=true
```

### Complex Queries

```http
GET /odata/Products?$filter=Price gt 100&$orderby=Price desc&$top=10&$select=Name,Price
GET /odata/Orders?$expand=Customer,OrderItems($expand=Product)&$filter=Status eq 'Pending'&$orderby=OrderDate desc
```

## Advanced Usage

### Custom OData Query Handler

```csharp
public class SearchProductsODataQuery : IQuery<ODataResult<Product>>
{
    public ODataQueryOptions<Product> QueryOptions { get; set; }
    public string SearchTerm { get; set; }
}

public class SearchProductsODataQueryHandler : IQueryHandler<SearchProductsODataQuery, ODataResult<Product>>
{
    private readonly ApplicationDbContext _context;

    public async Task<QueryResponse<ODataResult<Product>>> HandleAsync(
        SearchProductsODataQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<Product> products = _context.Products;

        // Apply custom filtering
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            products = products.Where(p =>
                p.Name.Contains(query.SearchTerm) ||
                p.Description.Contains(query.SearchTerm));
        }

        // Apply OData query options
        if (query.QueryOptions != null)
        {
            var settings = new ODataQuerySettings
            {
                PageSize = 20,
                EnableConstantParameterization = true
            };

            products = (IQueryable<Product>)query.QueryOptions.ApplyTo(products, settings);
        }

        var count = await products.CountAsync(cancellationToken);
        var items = await products.ToListAsync(cancellationToken);

        var result = new ODataResult<Product>
        {
            Items = items,
            Count = count
        };

        return QueryResponse<ODataResult<Product>>.Success(result);
    }
}
```

### OData with Caching

```csharp
public class GetProductsODataQuery : IQuery<IQueryable<Product>>, ICacheConfiguration, IGenerateCacheKey
{
    public ODataQueryOptions<Product> QueryOptions { get; set; }

    public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);

    public string GenerateCacheKey()
    {
        // Generate cache key from OData query string
        var queryString = QueryOptions?.Request?.QueryString.Value ?? string.Empty;
        return $"Products:OData:{queryString}";
    }
}
```

## Best Practices

### 1. Set Maximum Limits

```csharp
[EnableQuery(MaxTop = 100, PageSize = 20)]
public async Task<IActionResult> Get(ODataQueryOptions<Product> queryOptions)
{
    // Prevents clients from requesting too much data
}
```

### 2. Use AsNoTracking for Read-Only Queries

```csharp
public async Task<QueryResponse<IQueryable<Product>>> HandleAsync(...)
{
    IQueryable<Product> products = _context.Products.AsNoTracking();
    // Apply OData options...
}
```

### 3. Validate Query Options

```csharp
public async Task<IActionResult> Get(ODataQueryOptions<Product> queryOptions)
{
    try
    {
        queryOptions.Validate(new ODataValidationSettings
        {
            MaxTop = 100,
            MaxSkip = 1000,
            AllowedQueryOptions = AllowedQueryOptions.Filter | AllowedQueryOptions.OrderBy
        });
    }
    catch (ODataException ex)
    {
        return BadRequest(ex.Message);
    }

    // Process query...
}
```

### 4. Secure Sensitive Data

```csharp
[EnableQuery(AllowedQueryOptions = AllowedQueryOptions.Filter | AllowedQueryOptions.OrderBy)]
public async Task<IActionResult> Get()
{
    // Don't allow $select to prevent exposing sensitive fields
    // Or use [IgnoreDataMember] on sensitive properties
}
```

## Troubleshooting

### OData Routes Not Working

Ensure OData is properly configured:

```csharp
services.AddControllers()
    .AddOData(options => options
        .AddRouteComponents("odata", modelBuilder.GetEdmModel()));
```

### Query Options Not Applied

Check that `EnableQuery` attribute is present:

```csharp
[EnableQuery]
public async Task<IActionResult> Get(ODataQueryOptions<Product> queryOptions)
```

### Performance Issues

1. Set appropriate limits:
   ```csharp
   [EnableQuery(MaxTop = 100, PageSize = 20)]
   ```

2. Use AsNoTracking:
   ```csharp
   var products = _context.Products.AsNoTracking();
   ```

3. Add database indexes for frequently filtered/sorted fields

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.OData)
- [OData Documentation](https://www.odata.org/)
- [ASP.NET Core OData](https://docs.microsoft.com/en-us/odata/webapi/getting-started)
- [Main Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)

