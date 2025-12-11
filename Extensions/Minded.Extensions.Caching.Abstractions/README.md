# Minded.Extensions.Caching.Abstractions

Caching abstractions including cache key generation interfaces, configuration contracts, and attributes for the Minded Framework.

## Features

- **IGenerateCacheKey Interface** - Custom cache key generation contract
- **ICacheConfiguration Interface** - Cache duration configuration
- **Cache Attribute Definitions** - Attributes for cache control
- **Zero Dependencies** - Pure abstractions with no implementation dependencies

## Installation

```bash
dotnet add package Minded.Extensions.Caching.Abstractions
```

## Cache Attribute

### Overview

The `CacheAttribute` is an **abstract base class** for caching attributes. Concrete implementations (like `MemoryCacheAttribute`) derive from this class.

**IMPORTANT**: To enable caching, your query **MUST** be decorated with a caching attribute (e.g., `[MemoryCache]`) AND implement `IGenerateCacheKey`. Without the attribute, the query will not be cached.

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public abstract class CacheAttribute : Attribute
{
    /// <summary>
    /// Seconds from now when the cache entry will be evicted
    /// </summary>
    public int ExpirationInSeconds { get; set; }

    /// <summary>
    /// Seconds from last access when the cache entry will be evicted
    /// This will not extend the entry lifetime beyond the absolute expiration (if set)
    /// </summary>
    public int SlidingExpiration { get; set; }

    /// <summary>
    /// Date and time when the cache entry will be evicted.
    /// The accepted format is ISO 8601 representation 'e.g. 2023-06-30T12:00:00Z'.
    /// </summary>
    public string AbsoluteExpiration { get; set; }

    /// <summary>
    /// Errors are caught and suppressed by default, if caching is mandatory set this
    /// property to break the application flow and throw the exception
    /// </summary>
    public bool FailOnError { get; set; }
}
```

### Concrete Implementations

- **MemoryCacheAttribute** - In-memory caching (from `Minded.Extensions.Caching.Memory`)
- **DistributedCacheAttribute** - Distributed caching (if available)

**Example using MemoryCacheAttribute:**

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

## Interfaces

### IGenerateCacheKey

**REQUIRED** interface for all cached queries. Provides custom cache key generation logic:

```csharp
public interface IGenerateCacheKey
{
    /// <summary>
    /// Generate a unique cache key for this query
    /// </summary>
    string GetCacheKey();
}
```

**Example:**

```csharp
[MemoryCache(ExpirationInSeconds = 300)]
public class GetUserByIdQuery : IQuery<User>, IGenerateCacheKey
{
    public int UserId { get; set; }

    public string GetCacheKey() => $"User-{UserId}";
}
```

### Advanced IGenerateCacheKey Examples

#### Complex Cache Key with Multiple Properties

```csharp
[MemoryCache(ExpirationInSeconds = 600)]
public class SearchProductsQuery : IQuery<List<Product>>, IGenerateCacheKey
{
    public string SearchTerm { get; set; }
    public string Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    public string GetCacheKey()
    {
        var parts = new List<string>
        {
            $"Search:{SearchTerm?.ToLowerInvariant() ?? "all"}"
        };

        if (!string.IsNullOrEmpty(Category))
            parts.Add($"Cat:{Category}");

        if (MinPrice.HasValue)
            parts.Add($"Min:{MinPrice.Value}");

        if (MaxPrice.HasValue)
            parts.Add($"Max:{MaxPrice.Value}");

        return string.Join(":", parts);
    }
}
```

## Usage Examples

### Basic Caching

```csharp
[MemoryCache(ExpirationInSeconds = 86400)]  // 24 hours
public class GetCountriesQuery : IQuery<List<Country>>, IGenerateCacheKey
{
    // Cache for 24 hours - countries rarely change
    public string GetCacheKey() => "Countries";
}
```

### Conditional Caching with Dynamic Expiration

```csharp
// Note: For conditional caching, you can use different attributes or logic in the handler
[MemoryCache(ExpirationInSeconds = 600)]  // 10 minutes default
public class GetProductsQuery : IQuery<List<Product>>, IGenerateCacheKey
{
    public bool IncludeOutOfStock { get; set; }

    public string GetCacheKey()
    {
        // Include the flag in the cache key to cache separately
        return $"Products:OutOfStock:{IncludeOutOfStock}";
    }
}
```

### Custom Cache Key with Normalization

```csharp
[MemoryCache(ExpirationInSeconds = 300)]  // 5 minutes
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

### Complex Cache Key Generation

```csharp
[MemoryCache(ExpirationInSeconds = 180)]  // 3 minutes
public class GetOrdersQuery : IQuery<PagedResult<Order>>, IGenerateCacheKey
{
    public int CustomerId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public OrderStatus? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public string GetCacheKey()
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.Append($"Orders:Customer:{CustomerId}");

        if (StartDate.HasValue)
            keyBuilder.Append($":Start:{StartDate.Value:yyyy-MM-dd}");

        if (EndDate.HasValue)
            keyBuilder.Append($":End:{EndDate.Value:yyyy-MM-dd}");

        if (Status.HasValue)
            keyBuilder.Append($":Status:{Status.Value}");

        keyBuilder.Append($":Page:{PageNumber}:{PageSize}");

        return keyBuilder.ToString();
    }
}
```

## Best Practices

### 1. Always Use the Attribute and Interface Together

```csharp
// Good - attribute + interface
[MemoryCache(ExpirationInSeconds = 300)]
public class ComplexQuery : IQuery<Result>, IGenerateCacheKey
{
    public string GetCacheKey() => "custom-key";
}

// Bad - missing attribute (won't be cached!)
public class ComplexQuery : IQuery<Result>, IGenerateCacheKey
{
    public string GetCacheKey() => "custom-key";
}

// Bad - missing interface (will throw exception!)
[MemoryCache(ExpirationInSeconds = 300)]
public class ComplexQuery : IQuery<Result>
{
}
```

### 2. Normalize Keys for Case-Insensitive Data

```csharp
public string GetCacheKey()
{
    // Normalize to uppercase for consistency
    return $"Product:SKU:{SKU.ToUpperInvariant()}";
}
```

### 3. Include All Relevant Properties in Cache Key

```csharp
// Good - all properties that affect the result
public string GetCacheKey()
{
    return $"Search:{SearchTerm}:Page:{PageNumber}:Size:{PageSize}:Sort:{SortBy}";
}

// Avoid - missing properties
public string GetCacheKey()
{
    return $"Search:{SearchTerm}";  // Missing pagination and sorting!
}
```

### 4. Use Appropriate Cache Durations

```csharp
// Frequently changing data - short cache
[MemoryCache(ExpirationInSeconds = 30)]

// Rarely changing data - long cache
[MemoryCache(ExpirationInSeconds = 86400)]  // 24 hours

// User-specific data - medium cache
[MemoryCache(ExpirationInSeconds = 300)]  // 5 minutes
```

## Related Packages

- **Minded.Extensions.Caching.Memory** - In-memory caching implementation
- **Minded.Extensions.Caching.Distributed** - Distributed caching implementation (if available)

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/Minded.Extensions.Caching.Abstractions)
- [Minded.Extensions.Caching.Memory](https://www.nuget.org/packages/Minded.Extensions.Caching.Memory)
- [Main Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
