using System;

namespace Minded.Extensions.DataProtection.Abstractions
{
    /// <summary>
    /// Marks a property or field as containing sensitive data (PII, confidential business data, etc.).
    /// When applied, the property will be omitted from logs and exception messages unless explicitly enabled via configuration.
    /// This helps ensure compliance with data protection regulations (GDPR, CCPA, etc.) and prevents
    /// accidental exposure of sensitive information in log files and error messages.
    /// </summary>
    /// <remarks>
    /// Use this attribute on properties containing:
    /// - Personally Identifiable Information (PII): names, emails, phone numbers, addresses, SSN, etc.
    /// - Confidential business data: API keys, passwords, tokens, financial data, etc.
    /// - Any other data that should not appear in logs or exception messages by default
    ///
    /// By default, sensitive data is hidden. To show sensitive data (e.g., in development environments),
    /// configure DataProtectionOptions.ShowSensitiveData = true or use ShowSensitiveDataProvider for runtime control.
    /// 
    /// This attribute is part of the Data Protection abstractions and works with any IDataSanitizer implementation.
    /// </remarks>
    /// <example>
    /// <code>
    /// using Minded.Extensions.DataProtection.Abstractions;
    /// 
    /// public class CreateUserCommand
    /// {
    ///     public string Username { get; set; }
    ///
    ///     [SensitiveData]
    ///     public string Email { get; set; }
    ///
    ///     [SensitiveData]
    ///     public string Password { get; set; }
    ///
    ///     [SensitiveData]
    ///     public string CreditCardNumber { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class SensitiveDataAttribute : Attribute
    {
    }
}

