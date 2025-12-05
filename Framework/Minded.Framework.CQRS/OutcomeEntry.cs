using System.Text.Json.Serialization;

namespace Minded.Framework.CQRS.Abstractions
{
    /// <inheritdoc cref="IOutcomeEntry"/>
    public class OutcomeEntry : IOutcomeEntry
    {
        /// <summary>
        /// Creates a new OutcomeEntry.
        /// </summary>
        public OutcomeEntry(string propertyName, string message) : this(propertyName, message, null)
        {
        }

        /// <summary>
        /// Creates a new OutcomeEntry.
        /// </summary>
        [JsonConstructor]
        public OutcomeEntry(string propertyName, string message, object attemptedValue)
        {
            PropertyName = propertyName;
            Message = message;
            AttemptedValue = attemptedValue;
        }

        /// <inheritdoc cref="IOutcomeEntry.PropertyName"/>
        public string PropertyName { get; private set; }

        /// <inheritdoc cref="IOutcomeEntry.Message"/>
        public string Message { get; private set; }

        /// <inheritdoc cref="IOutcomeEntry.AttemptedValue"/>
        public object AttemptedValue { get; private set; }

        /// <inheritdoc cref="IOutcomeEntry.Severity"/>
        public Severity Severity { get; set; }

        /// <inheritdoc cref="IOutcomeEntry.ErrorCode"/>
        public string ErrorCode { get; set; }

        /// <inheritdoc cref="IOutcomeEntry.ResourceName"/>
        public string ResourceName { get; set; }

        /// <inheritdoc cref="IOutcomeEntry.UniqueErrorCode"/>
        public string UniqueErrorCode { get; set; }

        /// <inheritdoc cref="IOutcomeEntry.ToString"/>
        public override string ToString()
        {
            return Message;
        }

        /// <summary>
        /// Creates a Bad Request (400) outcome entry.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="propertyName">The name of the property that caused the error.</param>
        /// <param name="attemptedValue">The value that was attempted.</param>
        /// <param name="severity">The severity level of the outcome.</param>
        /// <param name="resourceName">The resource name used for building the message.</param>
        /// <returns>An <see cref="IOutcomeEntry"/> representing a Bad Request error.</returns>
        public static IOutcomeEntry BadRequest(string message, string propertyName = null, object attemptedValue = null, Severity severity = Severity.Error, string resourceName = null)
        {
            return WithStatusCode(message, propertyName, attemptedValue, 400, severity, resourceName);
        }

        /// <summary>
        /// Creates an Internal Server Error (500) outcome entry.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="propertyName">The name of the property that caused the error.</param>
        /// <param name="attemptedValue">The value that was attempted.</param>
        /// <param name="severity">The severity level of the outcome.</param>
        /// <param name="resourceName">The resource name used for building the message.</param>
        /// <returns>An <see cref="IOutcomeEntry"/> representing an Internal Server Error.</returns>
        public static IOutcomeEntry InternalServerError(string message, string propertyName = null, object attemptedValue = null, Severity severity = Severity.Error, string resourceName = null)
        {
            return WithStatusCode(message, propertyName, attemptedValue, 500, severity, resourceName);
        }

        /// <summary>
        /// Creates a Not Found (404) outcome entry.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="propertyName">The name of the property that caused the error.</param>
        /// <param name="attemptedValue">The value that was attempted.</param>
        /// <param name="severity">The severity level of the outcome.</param>
        /// <param name="resourceName">The resource name used for building the message.</param>
        /// <returns>An <see cref="IOutcomeEntry"/> representing a Not Found error.</returns>
        public static IOutcomeEntry NotFound(string message, string propertyName = null, object attemptedValue = null, Severity severity = Severity.Error, string resourceName = null)
        {
            return WithStatusCode(message, propertyName, attemptedValue, 404, severity, resourceName);
        }

        /// <summary>
        /// Creates an Unauthorized (401) outcome entry.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="propertyName">The name of the property that caused the error.</param>
        /// <param name="attemptedValue">The value that was attempted.</param>
        /// <param name="severity">The severity level of the outcome.</param>
        /// <param name="resourceName">The resource name used for building the message.</param>
        /// <returns>An <see cref="IOutcomeEntry"/> representing an Unauthorized error.</returns>
        public static IOutcomeEntry Unauthorized(string message, string propertyName = null, object attemptedValue = null, Severity severity = Severity.Error, string resourceName = null)
        {
            return WithStatusCode(message, propertyName, attemptedValue, 401, severity, resourceName);
        }

        /// <summary>
        /// Creates a Forbidden (403) outcome entry.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="propertyName">The name of the property that caused the error.</param>
        /// <param name="attemptedValue">The value that was attempted.</param>
        /// <param name="severity">The severity level of the outcome.</param>
        /// <param name="resourceName">The resource name used for building the message.</param>
        /// <returns>An <see cref="IOutcomeEntry"/> representing a Forbidden error.</returns>
        public static IOutcomeEntry Forbidden(string message, string propertyName = null, object attemptedValue = null, Severity severity = Severity.Error, string resourceName = null)
        {
            return WithStatusCode(message, propertyName, attemptedValue, 403, severity, resourceName);
        }

        /// <summary>
        /// Creates a Conflict (409) outcome entry.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="propertyName">The name of the property that caused the error.</param>
        /// <param name="attemptedValue">The value that was attempted.</param>
        /// <param name="severity">The severity level of the outcome.</param>
        /// <param name="resourceName">The resource name used for building the message.</param>
        /// <returns>An <see cref="IOutcomeEntry"/> representing a Conflict error.</returns>
        public static IOutcomeEntry Conflict(string message, string propertyName = null, object attemptedValue = null, Severity severity = Severity.Error, string resourceName = null)
        {
            return WithStatusCode(message, propertyName, attemptedValue, 409, severity, resourceName);
        }

        /// <summary>
        /// Creates an Unprocessable Entity (422) outcome entry.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="propertyName">The name of the property that caused the error.</param>
        /// <param name="attemptedValue">The value that was attempted.</param>
        /// <param name="severity">The severity level of the outcome.</param>
        /// <param name="resourceName">The resource name used for building the message.</param>
        /// <returns>An <see cref="IOutcomeEntry"/> representing an Unprocessable Entity error.</returns>
        public static IOutcomeEntry UnprocessableEntity(string message, string propertyName = null, object attemptedValue = null, Severity severity = Severity.Error, string resourceName = null)
        {
            return WithStatusCode(message, propertyName, attemptedValue, 422, severity, resourceName);
        }

        /// <summary>
        /// Creates a Service Unavailable (503) outcome entry.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="propertyName">The name of the property that caused the error.</param>
        /// <param name="attemptedValue">The value that was attempted.</param>
        /// <param name="severity">The severity level of the outcome.</param>
        /// <param name="resourceName">The resource name used for building the message.</param>
        /// <returns>An <see cref="IOutcomeEntry"/> representing a Service Unavailable error.</returns>
        public static IOutcomeEntry ServiceUnavailable(string message, string propertyName = null, object attemptedValue = null, Severity severity = Severity.Error, string resourceName = null)
        {
            return WithStatusCode(message, propertyName, attemptedValue, 503, severity, resourceName);
        }

        /// <summary>
        /// Creates an Ok (200) outcome entry.
        /// </summary>
        /// <param name="message">The informational message.</param>
        /// <param name="propertyName">The name of the property related to the outcome.</param>
        /// <param name="attemptedValue">The value that was processed.</param>
        /// <param name="severity">The severity level of the outcome.</param>
        /// <param name="resourceName">The resource name used for building the message.</param>
        /// <returns>An <see cref="IOutcomeEntry"/> representing a successful Ok response.</returns>
        public static IOutcomeEntry Ok(string message, string propertyName = null, object attemptedValue = null, Severity severity = Severity.Info, string resourceName = null)
        {
            return WithStatusCode(message, propertyName, attemptedValue, 200, severity, resourceName);
        }

        /// <summary>
        /// Creates a Created (201) outcome entry.
        /// </summary>
        /// <param name="message">The informational message.</param>
        /// <param name="propertyName">The name of the property related to the outcome.</param>
        /// <param name="attemptedValue">The value that was created.</param>
        /// <param name="severity">The severity level of the outcome.</param>
        /// <param name="resourceName">The resource name used for building the message.</param>
        /// <returns>An <see cref="IOutcomeEntry"/> representing a successful Created response.</returns>
        public static IOutcomeEntry Created(string message, string propertyName = null, object attemptedValue = null, Severity severity = Severity.Info, string resourceName = null)
        {
            return WithStatusCode(message, propertyName, attemptedValue, 201, severity, resourceName);
        }

        /// <summary>
        /// Creates an outcome entry with a custom HTTP status code.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="propertyName">The name of the property that caused the error.</param>
        /// <param name="attemptedValue">The value that was attempted.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="severity">The severity level of the outcome.</param>
        /// <param name="resourceName">The resource name used for building the message.</param>
        /// <returns>An <see cref="IOutcomeEntry"/> with the specified status code.</returns>
        public static IOutcomeEntry WithStatusCode(string message, string propertyName = null, object attemptedValue = null, int statusCode = 200, Severity severity = Severity.Error, string resourceName = null)
        {
            return new OutcomeEntry(propertyName, message, attemptedValue)
            {
                ErrorCode = statusCode.ToString(),
                Severity = severity,
                ResourceName = resourceName ?? propertyName
            };
        }
    }
}
