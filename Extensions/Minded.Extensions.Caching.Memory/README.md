# Minded.Extensions.Caching.Memory

In-memory caching decorator for query result caching with automatic cache key generation, configurable expiration. \
This decorator supports caching only for queries.

## Features

- **Automatic Query Result Caching** - Cache query results transparently
- **Configurable Cache Duration** - Set expiration per query type
- **Automatic Cache Key Generation** - Smart key generation based on query properties
- **Custom Cache Keys** - Implement `IGenerateCacheKey` for custom key logic
- **Memory Cache Integration** - Uses `IMemoryCache` from Microsoft.Extensions.Caching.Memory
- **Cache Invalidation** - Automatic expiration and manual invalidation support
- **Performance Optimization** - Reduce database calls or external service calls and improve response times

## Installation

```bash
dotnet add package Minded.Extensions.Caching.Memory
dotnet add package Microsoft.Extensions.Caching.Memory
```

## Quick Start

### 1. Create a Cacheable Query

**IMPORTANT**: To enable caching, your query **MUST** be decorated with the `[MemoryCache]` attribute AND implement `IGenerateCacheKey`. Without the attribute, the query will not be cached.

```csharp
using Minded.Framework.CQRS.Abstractions;
using Minded.Extensions.Caching.Abstractions.Decorator;
using Minded.Extensions.Caching.Memory.Decorator;

[MemoryCache(ExpirationInSeconds = 300)]  // Required attribute
public class GetUserByIdQuery : IQuery<User>, IGenerateCacheKey  // Required interface
{
    public int UserId { get; set; }

    // Required method to generate unique cache key
    public string GetCacheKey() => $"User-{UserId}";
}
```

### 2. Configure Caching Decorator

```csharp
using Minded.Extensions.Configuration;

services.AddMinded(builder =>
{
    // Add memory caching for queries
    builder.AddQueryCachingDecorator();
});

// Register IMemoryCache
services.AddMemoryCache();
```

### 3. Automatic Caching

```csharp
// First call - executes handler and caches result
var query1 = new GetUserByIdQuery { UserId = 123 };
var result1 = await _mediator.ProcessQueryAsync(query1);
// Handler executes, result cached for 5 minutes

// Second call within 5 minutes - returns cached result
var query2 = new GetUserByIdQuery { UserId = 123 };
var result2 = await _mediator.ProcessQueryAsync(query2);
// Handler NOT executed, cached result returned

// Different user ID - cache miss, executes handler
var query3 = new GetUserByIdQuery { UserId = 456 };
var result3 = await _mediator.ProcessQueryAsync(query3);
// Handler executes, new result cached
```

## Configuration Options

### Decorator Registration

The caching decorator can be registered with or without a custom global cache key prefix provider:

```csharp
// Default registration (no global prefix)
builder.AddQueryMemoryCacheDecorator();

// With custom global cache key prefix provider
builder.AddQueryMemoryCacheDecorator<MyCustomGlobalCacheKeyPrefixProvider>();
```

### IGlobalCacheKeyPrefixProvider

Implement this interface to add a global prefix to all cache keys (useful for multi-tenant scenarios or versioning):

```csharp
public interface IGlobalCacheKeyPrefixProvider
{
    string GetGlobalCacheKeyPrefix();
}
```

**Example:**

```csharp
public class TenantCacheKeyPrefixProvider : IGlobalCacheKeyPrefixProvider
{
    private readonly ITenantContext _tenantContext;

    public TenantCacheKeyPrefixProvider(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public string GetGlobalCacheKeyPrefix()
    {
        return $"Tenant-{_tenantContext.TenantId}:";
    }
}

// Registration
builder.AddQueryMemoryCacheDecorator<TenantCacheKeyPrefixProvider>();
```

### MemoryCache Attribute Properties

The `[MemoryCache]` attribute is **required** to enable caching for a query. Without this attribute, the query will not be cached, even if it implements `IGenerateCacheKey`.

**Requirements:**
1. Query must be decorated with `[MemoryCache]` attribute
2. Query must implement `IGenerateCacheKey` interface

**Attribute Definition:**

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class MemoryCacheAttribute : CacheAttribute
{
    public int ExpirationInSeconds { get; set; }
    public int SlidingExpiration { get; set; }
    public string AbsoluteExpiration { get; set; }
    public bool FailOnError { get; set; }
}
```

**Available Properties:**

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ExpirationInSeconds` | `int` | `0` | Seconds from now when the cache entry will be evicted (absolute expiration) |
| `SlidingExpiration` | `int` | `0` | Seconds from last access when the cache entry will be evicted. Does not extend beyond absolute expiration |
| `AbsoluteExpiration` | `string` | `null` | Exact date/time when cache entry expires (ISO 8601 format: '2023-06-30T12:00:00Z') |
| `FailOnError` | `bool` | `false` | If `true`, throws exception on caching errors. If `false` (default), suppresses errors and executes query normally |

### Property Details

#### ExpirationInSeconds

Specifies the number of seconds from now when the cache entry will be evicted (absolute expiration).

```csharp
// Cache for 5 minutes (300 seconds)
[MemoryCache(ExpirationInSeconds = 300)]
public class GetUserByIdQuery : IQuery<User>, IGenerateCacheKey
{
    public int UserId { get; set; }
    public string GetCacheKey() => $"User-{UserId}";
}
```

#### SlidingExpiration

Specifies the number of seconds from last access when the cache entry will be evicted. The entry will be evicted if it hasn't been accessed for this duration. This will not extend the entry lifetime beyond the absolute expiration (if set).

```csharp
// Cache expires 60 seconds after last access
[MemoryCache(SlidingExpiration = 60)]
public class GetRecentActivityQuery : IQuery<List<Activity>>, IGenerateCacheKey
{
    public int UserId { get; set; }
    public string GetCacheKey() => $"RecentActivity-{UserId}";
}
```

#### AbsoluteExpiration

Specifies the exact date and time when the cache entry will be evicted. The accepted format is ISO 8601 representation (e.g., '2023-06-30T12:00:00Z').

```csharp
// Cache until specific date/time
[MemoryCache(AbsoluteExpiration = "2024-12-31T00:00:00Z")]
public class GetDailyReportQuery : IQuery<Report>, IGenerateCacheKey
{
    public DateTime ReportDate { get; set; }
    public string GetCacheKey() => $"DailyReport-{ReportDate:yyyy-MM-dd}";
}
```

#### FailOnError

By default, caching errors are caught and suppressed, allowing the query to execute normally if caching fails. Set this property to `true` to break the application flow and throw the exception when caching errors occur.

```csharp
// Fail if caching fails (caching is mandatory)
[MemoryCache(ExpirationInSeconds = 300, FailOnError = true)]
public class GetCriticalDataQuery : IQuery<CriticalData>, IGenerateCacheKey
{
    public string GetCacheKey() => "CriticalData";
}
```

### Combining Multiple Options

You can combine multiple cache options:

```csharp
// Absolute expiration of 1 hour, but also sliding expiration of 5 minutes
// Entry expires after 1 hour OR 5 minutes of inactivity, whichever comes first
[MemoryCache(ExpirationInSeconds = 3600, SlidingExpiration = 300)]
public class GetProductCatalogQuery : IQuery<List<Product>>, IGenerateCacheKey
{
    public string Category { get; set; }
    public string GetCacheKey() => $"ProductCatalog-{Category}";
}
```

### Default Behavior

If no expiration options are specified, the cache entry will use the default behavior of `IMemoryCache`:

```csharp
// Uses default cache behavior (no explicit expiration)
[MemoryCache]
public class GetConfigQuery : IQuery<Config>, IGenerateCacheKey
{
    public string GetCacheKey() => "AppConfig";
}
```

### Cache Duration Examples

```csharp
// Short-lived cache (30 seconds)
[MemoryCache(ExpirationInSeconds = 30)]
public class GetRealtimeDataQuery : IQuery<Data>, IGenerateCacheKey
{
    public string GetCacheKey() => "RealtimeData";
}

// Medium-lived cache (5 minutes)
[MemoryCache(ExpirationInSeconds = 300)]
public class GetUserQuery : IQuery<User>, IGenerateCacheKey
{
    public int UserId { get; set; }
    public string GetCacheKey() => $"User-{UserId}";
}

// Long-lived cache (1 hour)
[MemoryCache(ExpirationInSeconds = 3600)]
public class GetCountriesQuery : IQuery<List<Country>>, IGenerateCacheKey
{
    public string GetCacheKey() => "Countries";
}

// Very long-lived cache (24 hours)
[MemoryCache(ExpirationInSeconds = 86400)]
public class GetStaticConfigQuery : IQuery<Config>, IGenerateCacheKey
{
    public string GetCacheKey() => "StaticConfig";
}

// No caching (don't add the attribute)
public class GetCurrentTimeQuery : IQuery<DateTime>
{
    // No caching - always executes handler
}
```

## Cache Key Generation

### Custom Cache Key Generation

All cached queries **MUST** implement `IGenerateCacheKey` to provide unique cache keys:

```csharp
using Minded.Extensions.Caching.Abstractions.Decorator;
using Minded.Extensions.Caching.Memory.Decorator;

[MemoryCache(ExpirationInSeconds = 300)]
public class GetUserByIdQuery : IQuery<User>, IGenerateCacheKey
{
    public int UserId { get; set; }

    public string GetCacheKey() => $"User-{UserId}";
}
```

### Multiple Properties in Cache Key

```csharp
[MemoryCache(ExpirationInSeconds = 120)]
public class SearchUsersQuery : IQuery<List<User>>, IGenerateCacheKey
{
    public string SearchTerm { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    public string GetCacheKey()
    {
        return $"SearchUsers:{SearchTerm}:Page{PageNumber}:Size{PageSize}";
    }
}
```

### Normalized Cache Keys

Normalize keys for case-insensitive data:

```csharp
[MemoryCache(ExpirationInSeconds = 300)]
public class GetUserByEmailQuery : IQuery<User>, IGenerateCacheKey
{
    public string Email { get; set; }

    public string GetCacheKey()
    {
        // Normalize email to lowercase for consistent caching
        return $"User:Email:{Email.ToLowerInvariant()}";
    }
}
```

### Complex Cache Keys

```csharp
[MemoryCache(ExpirationInSeconds = 180)]
public class GetOrdersQuery : IQuery<List<Order>>, IGenerateCacheKey
{
    public int CustomerId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public OrderStatus? Status { get; set; }

    public string GetCacheKey()
    {
        var parts = new List<string>
        {
            $"CustomerId:{CustomerId}"
        };

        if (StartDate.HasValue)
            parts.Add($"Start:{StartDate.Value:yyyy-MM-dd}");

        if (EndDate.HasValue)
            parts.Add($"End:{EndDate.Value:yyyy-MM-dd}");

        if (Status.HasValue)
            parts.Add($"Status:{Status.Value}");

        return $"Orders:{string.Join(":", parts)}";
    }
}
```

## Advanced Usage

### Conditional Caching with Different Keys

For conditional caching, include the condition in the cache key to cache different results separately:

```csharp
[MemoryCache(ExpirationInSeconds = 600)]
public class GetProductsQuery : IQuery<List<Product>>, IGenerateCacheKey
{
    public string Category { get; set; }
    public bool IncludeOutOfStock { get; set; }

    public string GetCacheKey()
    {
        // Include the flag in the cache key to cache separately
        return $"Products:{Category}:OutOfStock:{IncludeOutOfStock}";
    }
}
```

### Cache Warming

Pre-populate cache with frequently accessed data:

```csharp
public class CacheWarmingService : IHostedService
{
    private readonly IMediator _mediator;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Warm up cache on application startup
        await _mediator.ProcessQueryAsync(new GetCountriesQuery(), cancellationToken);
        await _mediator.ProcessQueryAsync(new GetCategoriesQuery(), cancellationToken);
        await _mediator.ProcessQueryAsync(new GetStaticConfigQuery(), cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

### Cache Invalidation

Manual cache invalidation using `IMemoryCache`:

```csharp
public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, User>
{
    private readonly IUserRepository _repository;
    private readonly IMemoryCache _cache;

    public async Task<CommandResponse<User>> HandleAsync(
        UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _repository.UpdateAsync(command.User, cancellationToken);

        // Invalidate cached user data
        _cache.Remove($"GetUserByIdQuery:UserId={user.Id}");
        _cache.Remove($"User:Email:{user.Email.ToLowerInvariant()}");

        return CommandResponse<User>.Success(user);
    }
}
```

### Distributed Caching Pattern

For multi-server scenarios, consider using distributed cache:

```csharp
// Use Redis or SQL Server distributed cache instead
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = Configuration.GetConnectionString("Redis");
});

// Then use Minded.Extensions.Caching.Distributed (if available)
// or implement custom caching decorator
```

## Best Practices

### 1. Cache Read-Only Queries

```csharp
// Good - cache read-only queries
[MemoryCache(ExpirationInSeconds = 300)]
public class GetUserByIdQuery : IQuery<User>, IGenerateCacheKey
{
    public int UserId { get; set; }
    public string GetCacheKey() => $"User-{UserId}";
}

// Avoid - don't cache commands (they modify state)
public class CreateUserCommand : ICommand<User> { }  // No caching attribute
```

### 2. Choose Appropriate Cache Durations

```csharp
// Frequently changing data - short cache (10 seconds)
[MemoryCache(ExpirationInSeconds = 10)]
public class GetStockPriceQuery : IQuery<decimal>, IGenerateCacheKey
{
    public string Symbol { get; set; }
    public string GetCacheKey() => $"StockPrice-{Symbol}";
}

// Rarely changing data - long cache (24 hours)
[MemoryCache(ExpirationInSeconds = 86400)]
public class GetCountriesQuery : IQuery<List<Country>>, IGenerateCacheKey
{
    public string GetCacheKey() => "Countries";
}

// User-specific data - medium cache (15 minutes)
[MemoryCache(ExpirationInSeconds = 900)]
public class GetUserPreferencesQuery : IQuery<Preferences>, IGenerateCacheKey
{
    public int UserId { get; set; }
    public string GetCacheKey() => $"UserPreferences-{UserId}";
}
```

### 3. Include All Relevant Properties in Cache Key

```csharp
// Good - all properties affect the result
[MemoryCache(ExpirationInSeconds = 300)]
public class SearchProductsQuery : IQuery<List<Product>>, IGenerateCacheKey
{
    public string SearchTerm { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string SortBy { get; set; }

    public string GetCacheKey()
    {
        return $"Products:{SearchTerm}:Page{PageNumber}:Size{PageSize}:Sort{SortBy}";
    }
}

// Avoid - missing properties that affect results
[MemoryCache(ExpirationInSeconds = 300)]
public class SearchProductsQuery : IQuery<List<Product>>, IGenerateCacheKey
{
    public string SearchTerm { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string SortBy { get; set; }

    public string GetCacheKey()
    {
        // Missing PageSize and SortBy - cache collisions!
        return $"Products:{SearchTerm}:Page{PageNumber}";
    }
}
```

### 4. Invalidate Cache on Updates

```csharp
public class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, Product>
{
    private readonly IMemoryCache _cache;

    public async Task<CommandResponse<Product>> HandleAsync(...)
    {
        var product = await _repository.UpdateAsync(command.Product);

        // Invalidate all related cache entries
        _cache.Remove($"GetProductByIdQuery:ProductId={product.Id}");
        _cache.Remove($"GetProductsByCategoryQuery:CategoryId={product.CategoryId}");

        return CommandResponse<Product>.Success(product);
    }
}
```

## Performance Considerations

### Memory Usage

```csharp
// Configure memory cache limits
services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // Limit cache size
    options.CompactionPercentage = 0.25; // Compact when 75% full
});

// Note: Size-aware caching is configured at the IMemoryCache level
// Individual queries use the attribute for expiration settings
```

### Cache Eviction

The `[MemoryCache]` attribute supports multiple expiration strategies:

```csharp
// Absolute expiration - evict after fixed time
[MemoryCache(ExpirationInSeconds = 3600)]  // 1 hour

// Sliding expiration - evict after inactivity
[MemoryCache(SlidingExpiration = 300)]  // 5 minutes of inactivity

// Combination - evict after 1 hour OR 5 minutes of inactivity
[MemoryCache(ExpirationInSeconds = 3600, SlidingExpiration = 300)]

// Specific date/time expiration
[MemoryCache(AbsoluteExpiration = "2024-12-31T23:59:59Z")]
```

### Avoid Caching Large Objects

```csharp
// Avoid - caching large result sets
[MemoryCache(ExpirationInSeconds = 3600)]
public class GetAllUsersQuery : IQuery<List<User>>, IGenerateCacheKey
{
    // Could cache millions of users - bad for memory
    public string GetCacheKey() => "AllUsers";
}

// Good - cache smaller, paginated results
[MemoryCache(ExpirationInSeconds = 300)]
public class GetUsersPageQuery : IQuery<List<User>>, IGenerateCacheKey
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; } = 20;

    public string GetCacheKey() => $"Users:Page{PageNumber}:Size{PageSize}";
}
```

## Troubleshooting

### Cache Not Working

1. **Ensure the `[MemoryCache]` attribute is present on your query**:
   ```csharp
   [MemoryCache(ExpirationInSeconds = 300)]  // Required!
   public class MyQuery : IQuery<Result>, IGenerateCacheKey
   {
       public string GetCacheKey() => "MyKey";
   }
   ```

2. **Verify your query implements `IGenerateCacheKey`**:
   ```csharp
   public class MyQuery : IQuery<Result>, IGenerateCacheKey
   {
       public string GetCacheKey() => "UniqueKey";
   }
   ```

3. Ensure the caching decorator is registered:
   ```csharp
   builder.AddQueryMemoryCacheDecorator();
   ```

4. Verify `IMemoryCache` is registered:
   ```csharp
   services.AddMemoryCache();
   ```

### Cache Keys Colliding

If different queries return the same cached result:

1. Implement unique cache keys in `GetCacheKey()`:
   ```csharp
   public string GetCacheKey()
   {
       return $"MyQuery:{Property1}:{Property2}";
   }
   ```

2. Ensure all relevant properties are included in the key

### Memory Issues

If cache is consuming too much memory:

1. Reduce cache durations:
   ```csharp
   public TimeSpan CacheDuration => TimeSpan.FromMinutes(1); // Reduced
   ```

2. Configure memory limits:
   ```csharp
   services.AddMemoryCache(options =>
   {
       options.SizeLimit = 512; // Reduce limit
   });
   ```

3. Don't cache large objects or result sets

## Integration with Other Decorators

### Decorator Order

```csharp
services.AddMinded(builder =>
{
    builder.AddQueryExceptionDecorator();   // First - catch exceptions
    builder.AddQueryLoggingDecorator();     // Second - log requests
    builder.AddQueryValidationDecorator();  // Third - validate
    builder.AddQueryCachingDecorator();     // Fourth - check cache
    // Handler executes last (if cache miss)
});
```

## Integration with Other Decorators

### With Logging Decorator

The Logging decorator logs cache hits and misses:

```csharp
builder.AddQueryMemoryCacheDecorator()
       .AddQueryLoggingDecorator()    // Logs cache operations
       .AddQueryExceptionDecorator()
       .AddQueryHandlers();
```

See: [Logging Decorator Documentation](../Minded.Extensions.Logging/README.md)

### With Exception Decorator

The Exception decorator catches cache-related errors:

```csharp
builder.AddQueryMemoryCacheDecorator()
       .AddQueryExceptionDecorator()  // Catches cache errors
       .AddQueryHandlers();
```

See: [Exception Decorator Documentation](../Minded.Extensions.Exception/README.md)

### With RestMediator

RestMediator returns cached results with HTTP 200 OK:

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(int id)
{
    // Cached results automatically returned with 200 OK
    return await _restMediator.ProcessRestQueryAsync(
        RestOperation.GetSingle,
        new GetUserByIdQuery(id));
}
```

See: [RestMediator Documentation](../Minded.Extensions.WebApi/README.md)

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.Caching.Memory)
- [Minded.Extensions.Caching.Abstractions](https://www.nuget.org/packages/Minded.Extensions.Caching.Abstractions)
- [Main Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
