using System;
using System.Collections.Generic;
using System.Linq;
using Minded.Framework.CQRS.Abstractions;

namespace Minded.Framework.CQRS.Query
{
    /// <summary>
    /// Query response object
    /// </summary>
    /// <typeparam name="TResult">Query result</typeparam>
    public class QueryResponse<TResult> : IQueryResponse<TResult>
    {
        public QueryResponse() => OutcomeEntries = new List<IOutcomeEntry>();

        /// <summary>
        /// Constructor that creates a successful query response with a result
        /// </summary>
        /// <param name="result">The query result</param>
        public QueryResponse(TResult result) : this()
        {
            Result = result;
            Successful = true;
        }

        public QueryResponse(params IOutcomeEntry[] outcomeEntries) : this()
        {
            OutcomeEntries = new List<IOutcomeEntry>(outcomeEntries);
            Successful = false; // Set to false by default when only outcome entries are provided
        }

        /// <summary>
        /// Constructor that accepts a success flag and optional outcome entries
        /// </summary>
        /// <param name="successful">Indicates whether the query was successful</param>
        /// <param name="outcomeEntries">Optional outcome entries</param>
        public QueryResponse(bool successful, params IOutcomeEntry[] outcomeEntries) : this(outcomeEntries)
        {
            Successful = successful;
        }

        /// <summary>
        /// Constructor that accepts a result and success flag
        /// </summary>
        /// <param name="result">The query result</param>
        /// <param name="successful">Indicates whether the query was successful</param>
        public QueryResponse(TResult result, bool successful) : this()
        {
            Result = result;
            Successful = successful;
        }

        /// <summary>
        /// Constructor that accepts a result, success flag, and outcome entries
        /// </summary>
        /// <param name="result">The query result</param>
        /// <param name="successful">Indicates whether the query was successful</param>
        /// <param name="outcomeEntries">Optional outcome entries</param>
        public QueryResponse(TResult result, bool successful, params IOutcomeEntry[] outcomeEntries) : this(outcomeEntries)
        {
            Result = result;
            Successful = successful;
        }

        public TResult Result { get; }

        public bool Successful { get; set; }

        public List<IOutcomeEntry> OutcomeEntries { get; set; }

        /// <summary>
        /// Creates a successful query response with a result and optional outcome entries
        /// </summary>
        /// <param name="result">The query result</param>
        /// <param name="outcomeEntries">Optional outcome entries</param>
        /// <returns>A successful query response with the specified result</returns>
        /// <example>
        /// <code>
        /// var categories = await _context.Categories.ToListAsync();
        /// return QueryResponse&lt;List&lt;Category&gt;&gt;.Success(categories);
        /// </code>
        /// </example>
        public static QueryResponse<TResult> Success(TResult result, params IOutcomeEntry[] outcomeEntries)
            => new QueryResponse<TResult>(result, successful: true, outcomeEntries);

        /// <summary>
        /// Creates an error query response with the specified outcome entries
        /// </summary>
        /// <param name="outcomeEntries">The outcome entries describing the errors</param>
        /// <returns>An error query response</returns>
        /// <example>
        /// <code>
        /// return QueryResponse&lt;Category&gt;.Error(
        ///     OutcomeEntry.NotFound("Category not found", nameof(categoryId))
        /// );
        /// </code>
        /// </example>
        public static QueryResponse<TResult> Error(params IOutcomeEntry[] outcomeEntries)
            => new QueryResponse<TResult>(successful: false, outcomeEntries);

        /// <summary>
        /// Creates a Not Found (404) error query response
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="propertyName">The name of the property that caused the error</param>
        /// <param name="attemptedValue">The value that was attempted</param>
        /// <param name="severity">The severity level of the outcome</param>
        /// <param name="resourceName">The resource name used for building the message</param>
        /// <returns>An error query response with a Not Found outcome entry</returns>
        /// <example>
        /// <code>
        /// return QueryResponse&lt;Category&gt;.NotFound("Category not found", nameof(categoryId), categoryId);
        /// </code>
        /// </example>
        public static QueryResponse<TResult> NotFound(string message, string propertyName = null, object attemptedValue = null, Severity severity = Severity.Error, string resourceName = null)
            => Error(OutcomeEntry.NotFound(message, propertyName, attemptedValue, severity, resourceName));

        /// <summary>
        /// Creates a Bad Request (400) error query response
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="propertyName">The name of the property that caused the error</param>
        /// <param name="attemptedValue">The value that was attempted</param>
        /// <param name="severity">The severity level of the outcome</param>
        /// <param name="resourceName">The resource name used for building the message</param>
        /// <returns>An error query response with a Bad Request outcome entry</returns>
        public static QueryResponse<TResult> BadRequest(string message, string propertyName = null, object attemptedValue = null, Severity severity = Severity.Error, string resourceName = null)
            => Error(OutcomeEntry.BadRequest(message, propertyName, attemptedValue, severity, resourceName));

        /// <summary>
        /// Creates an Unauthorized (401) error query response
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="propertyName">The name of the property that caused the error</param>
        /// <param name="attemptedValue">The value that was attempted</param>
        /// <param name="severity">The severity level of the outcome</param>
        /// <param name="resourceName">The resource name used for building the message</param>
        /// <returns>An error query response with an Unauthorized outcome entry</returns>
        public static QueryResponse<TResult> Unauthorized(string message, string propertyName = null, object attemptedValue = null, Severity severity = Severity.Error, string resourceName = null)
            => Error(OutcomeEntry.Unauthorized(message, propertyName, attemptedValue, severity, resourceName));

        /// <summary>
        /// Creates a Forbidden (403) error query response
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="propertyName">The name of the property that caused the error</param>
        /// <param name="attemptedValue">The value that was attempted</param>
        /// <param name="severity">The severity level of the outcome</param>
        /// <param name="resourceName">The resource name used for building the message</param>
        /// <returns>An error query response with a Forbidden outcome entry</returns>
        public static QueryResponse<TResult> Forbidden(string message, string propertyName = null, object attemptedValue = null, Severity severity = Severity.Error, string resourceName = null)
            => Error(OutcomeEntry.Forbidden(message, propertyName, attemptedValue, severity, resourceName));

        /// <summary>
        /// Creates a Conflict (409) error query response
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="propertyName">The name of the property that caused the error</param>
        /// <param name="attemptedValue">The value that was attempted</param>
        /// <param name="severity">The severity level of the outcome</param>
        /// <param name="resourceName">The resource name used for building the message</param>
        /// <returns>An error query response with a Conflict outcome entry</returns>
        public static QueryResponse<TResult> Conflict(string message, string propertyName = null, object attemptedValue = null, Severity severity = Severity.Error, string resourceName = null)
            => Error(OutcomeEntry.Conflict(message, propertyName, attemptedValue, severity, resourceName));

        /// <summary>
        /// Creates an Internal Server Error (500) error query response
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="propertyName">The name of the property that caused the error</param>
        /// <param name="attemptedValue">The value that was attempted</param>
        /// <param name="severity">The severity level of the outcome</param>
        /// <param name="resourceName">The resource name used for building the message</param>
        /// <returns>An error query response with an Internal Server Error outcome entry</returns>
        public static QueryResponse<TResult> InternalServerError(string message, string propertyName = null, object attemptedValue = null, Severity severity = Severity.Error, string resourceName = null)
            => Error(OutcomeEntry.InternalServerError(message, propertyName, attemptedValue, severity, resourceName));

        /// <summary>
        /// Adds an outcome entry to the response and returns the response for method chaining
        /// </summary>
        /// <param name="entry">The outcome entry to add</param>
        /// <returns>The current query response instance for fluent chaining</returns>
        public QueryResponse<TResult> WithOutcome(IOutcomeEntry entry)
        {
            OutcomeEntries.Add(entry);
            return this;
        }

        /// <summary>
        /// Adds multiple outcome entries to the response and returns the response for method chaining
        /// </summary>
        /// <param name="entries">The outcome entries to add</param>
        /// <returns>The current query response instance for fluent chaining</returns>
        public QueryResponse<TResult> WithOutcomes(params IOutcomeEntry[] entries)
        {
            if (entries != null && entries.Length > 0)
            {
                OutcomeEntries.AddRange(entries);
            }
            return this;
        }
    }
}
