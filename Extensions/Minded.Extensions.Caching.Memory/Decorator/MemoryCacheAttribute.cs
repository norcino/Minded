using System;
using Minded.Extensions.Caching.Abstractions.Decorator;

namespace Minded.Extensions.Caching.Memory.Decorator
{
    /// <summary>
    /// Caches the query result using the given cofiguration
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MemoryCacheAttribute : CacheAttribute
    {
    }
}
