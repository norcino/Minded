using System;

namespace Minded.Extensions.Exception
{
    /// <summary>
    /// Marks a property to be excluded from serialized diagnostic logging in exception handlers.
    /// Properties marked with this attribute will be omitted when the exception decorator serializes
    /// commands or queries for logging purposes.
    /// </summary>
    /// <remarks>
    /// Use this attribute on properties that:
    /// - Cannot be serialized (e.g., CancellationToken, Task, Func, Action, Stream)
    /// - Should not appear in diagnostic logs for any reason
    /// - Cause serialization issues due to circular references or complex object graphs
    /// 
    /// This attribute works alongside [JsonIgnore] - properties with either attribute will be excluded.
    /// 
    /// Note: For sensitive data protection (PII, passwords, etc.), use [SensitiveData] attribute instead,
    /// which integrates with the DataProtection system for configurable visibility.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class ProcessFileCommand : ICommand
    /// {
    ///     public Guid TraceId { get; set; }
    ///     public string FileName { get; set; }
    ///     
    ///     [ExcludeFromSerializedDiagnosticLogging]
    ///     public Stream FileStream { get; set; }
    ///     
    ///     [ExcludeFromSerializedDiagnosticLogging]
    ///     public CancellationToken CancellationToken { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ExcludeFromSerializedDiagnosticLoggingAttribute : Attribute
    {
    }
}

