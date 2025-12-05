using System;
using System.Collections.Generic;
using System.Linq;
using Minded.Framework.CQRS.Abstractions;

namespace Minded.Framework.CQRS.Command
{
    /// <summary>
    /// Base command response containing the status of the response
    /// </summary>
    public class CommandResponse : ICommandResponse
    {
        public bool Successful { get; set; }

        public List<IOutcomeEntry> OutcomeEntries { get; set; }

        public CommandResponse()
        {
            OutcomeEntries = new List<IOutcomeEntry>();
        }

        public CommandResponse(params IOutcomeEntry[] outcomeEntries) : this()
        {
            OutcomeEntries = new List<IOutcomeEntry>(outcomeEntries);
            Successful = false; // Default to false when only outcome entries are provided
        }

        /// <summary>
        /// Constructor that accepts a success flag and optional outcome entries
        /// </summary>
        /// <param name="successful">Indicates whether the command was successful</param>
        /// <param name="outcomeEntries">Optional outcome entries</param>
        public CommandResponse(bool successful, params IOutcomeEntry[] outcomeEntries) : this(outcomeEntries)
        {
            Successful = successful;
        }

        /// <summary>
        /// Creates a successful command response with optional outcome entries
        /// </summary>
        /// <param name="outcomeEntries">Optional outcome entries</param>
        /// <returns>A successful command response</returns>
        public static CommandResponse Success(params IOutcomeEntry[] outcomeEntries)
            => new CommandResponse(successful: true, outcomeEntries);

        /// <summary>
        /// Creates an error command response with the specified outcome entries
        /// </summary>
        /// <param name="outcomeEntries">The outcome entries describing the errors</param>
        /// <returns>An error command response</returns>
        public static CommandResponse Error(params IOutcomeEntry[] outcomeEntries)
            => new CommandResponse(successful: false, outcomeEntries);

        /// <summary>
        /// Adds an outcome entry to the response and returns the response for method chaining
        /// </summary>
        /// <param name="entry">The outcome entry to add</param>
        /// <returns>The current command response instance for fluent chaining</returns>
        public CommandResponse WithOutcome(IOutcomeEntry entry)
        {
            OutcomeEntries.Add(entry);
            return this;
        }

        /// <summary>
        /// Adds multiple outcome entries to the response and returns the response for method chaining
        /// </summary>
        /// <param name="entries">The outcome entries to add</param>
        /// <returns>The current command response instance for fluent chaining</returns>
        public CommandResponse WithOutcomes(params IOutcomeEntry[] entries)
        {
            if (entries != null && entries.Length > 0)
            {
                OutcomeEntries.AddRange(entries);
            }
            return this;
        }

        /// <summary>
        /// Creates a NotFound (404) error response with the specified message
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="propertyName">Optional property name associated with the error</param>
        /// <param name="attemptedValue">Optional attempted value that caused the error</param>
        /// <returns>A command response with NotFound outcome entry</returns>
        public static CommandResponse NotFound(string message, string propertyName = null, object attemptedValue = null)
            => new CommandResponse(successful: false, OutcomeEntry.NotFound(message, propertyName, attemptedValue));

        /// <summary>
        /// Creates a BadRequest (400) error response with the specified message
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="propertyName">Optional property name associated with the error</param>
        /// <param name="attemptedValue">Optional attempted value that caused the error</param>
        /// <returns>A command response with BadRequest outcome entry</returns>
        public static CommandResponse BadRequest(string message, string propertyName = null, object attemptedValue = null)
            => new CommandResponse(successful: false, OutcomeEntry.BadRequest(message, propertyName, attemptedValue));

        /// <summary>
        /// Creates an Unauthorized (401) error response with the specified message
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="propertyName">Optional property name associated with the error</param>
        /// <param name="attemptedValue">Optional attempted value that caused the error</param>
        /// <returns>A command response with Unauthorized outcome entry</returns>
        public static CommandResponse Unauthorized(string message, string propertyName = null, object attemptedValue = null)
            => new CommandResponse(successful: false, OutcomeEntry.Unauthorized(message, propertyName, attemptedValue));

        /// <summary>
        /// Creates a Forbidden (403) error response with the specified message
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="propertyName">Optional property name associated with the error</param>
        /// <param name="attemptedValue">Optional attempted value that caused the error</param>
        /// <returns>A command response with Forbidden outcome entry</returns>
        public static CommandResponse Forbidden(string message, string propertyName = null, object attemptedValue = null)
            => new CommandResponse(successful: false, OutcomeEntry.Forbidden(message, propertyName, attemptedValue));

        /// <summary>
        /// Creates a Conflict (409) error response with the specified message
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="propertyName">Optional property name associated with the error</param>
        /// <param name="attemptedValue">Optional attempted value that caused the error</param>
        /// <returns>A command response with Conflict outcome entry</returns>
        public static CommandResponse Conflict(string message, string propertyName = null, object attemptedValue = null)
            => new CommandResponse(successful: false, OutcomeEntry.Conflict(message, propertyName, attemptedValue));

        /// <summary>
        /// Creates an InternalServerError (500) error response with the specified message
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="propertyName">Optional property name associated with the error</param>
        /// <param name="attemptedValue">Optional attempted value that caused the error</param>
        /// <returns>A command response with InternalServerError outcome entry</returns>
        public static CommandResponse InternalServerError(string message, string propertyName = null, object attemptedValue = null)
            => new CommandResponse(successful: false, OutcomeEntry.InternalServerError(message, propertyName, attemptedValue));
    }

    /// <summary>
    /// Command response object containing the command result
    /// </summary>
    /// <typeparam name="TResult">Result type</typeparam>
    public class CommandResponse<TResult> : CommandResponse, ICommandResponse<TResult>
    {
        public TResult Result { get; }

        public CommandResponse() : base() { }

        public CommandResponse(params IOutcomeEntry[] outcomeEntries) : base(outcomeEntries)
        {
            Successful = false; // Default to false when only outcome entries are provided
        }

        /// <summary>
        /// Constructor that accepts a success flag and optional outcome entries
        /// </summary>
        /// <param name="successful">Indicates whether the command was successful</param>
        /// <param name="outcomeEntries">Optional outcome entries</param>
        public CommandResponse(bool successful, params IOutcomeEntry[] outcomeEntries) : base(successful, outcomeEntries)
        {
        }

        /// <summary>
        /// Constructor that creates a successful command response with a result
        /// </summary>
        /// <param name="result">The command result</param>
        public CommandResponse(TResult result) : base()
        {
            Result = result;
            Successful = true; // Set success to true by default when a result is provided
        }

        /// <summary>
        /// Constructor that accepts a result and success flag
        /// </summary>
        /// <param name="result">The command result</param>
        /// <param name="successful">Indicates whether the command was successful</param>
        public CommandResponse(TResult result, bool successful) : base()
        {
            Result = result;
            Successful = successful;
        }

        /// <summary>
        /// Constructor that accepts a result, success flag, and outcome entries
        /// </summary>
        /// <param name="result">The command result</param>
        /// <param name="successful">Indicates whether the command was successful</param>
        /// <param name="outcomeEntries">Optional outcome entries</param>
        public CommandResponse(TResult result, bool successful, params IOutcomeEntry[] outcomeEntries) : base(outcomeEntries)
        {
            Result = result;
            Successful = successful;
        }

        /// <summary>
        /// Creates a successful command response with a result and optional outcome entries
        /// </summary>
        /// <param name="result">The command result</param>
        /// <param name="outcomeEntries">Optional outcome entries</param>
        /// <returns>A successful command response with the specified result</returns>
        public static CommandResponse<TResult> Success(TResult result, params IOutcomeEntry[] outcomeEntries)
            => new CommandResponse<TResult>(result, successful: true, outcomeEntries);

        /// <summary>
        /// Creates an error command response with the specified outcome entries
        /// </summary>
        /// <param name="outcomeEntries">The outcome entries describing the errors</param>
        /// <returns>An error command response</returns>
        public static new CommandResponse<TResult> Error(params IOutcomeEntry[] outcomeEntries)
            => new CommandResponse<TResult>(successful: false, outcomeEntries);

        /// <summary>
        /// Adds an outcome entry to the response and returns the response for method chaining
        /// </summary>
        /// <param name="entry">The outcome entry to add</param>
        /// <returns>The current command response instance for fluent chaining</returns>
        public new CommandResponse<TResult> WithOutcome(IOutcomeEntry entry)
        {
            OutcomeEntries.Add(entry);
            return this;
        }

        /// <summary>
        /// Adds multiple outcome entries to the response and returns the response for method chaining
        /// </summary>
        /// <param name="entries">The outcome entries to add</param>
        /// <returns>The current command response instance for fluent chaining</returns>
        public new CommandResponse<TResult> WithOutcomes(params IOutcomeEntry[] entries)
        {
            if (entries != null && entries.Length > 0)
            {
                OutcomeEntries.AddRange(entries);
            }
            return this;
        }

        /// <summary>
        /// Creates a NotFound (404) error response with the specified message
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="propertyName">Optional property name associated with the error</param>
        /// <param name="attemptedValue">Optional attempted value that caused the error</param>
        /// <returns>A command response with NotFound outcome entry</returns>
        public static new CommandResponse<TResult> NotFound(string message, string propertyName = null, object attemptedValue = null)
            => new CommandResponse<TResult>(successful: false, OutcomeEntry.NotFound(message, propertyName, attemptedValue));

        /// <summary>
        /// Creates a BadRequest (400) error response with the specified message
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="propertyName">Optional property name associated with the error</param>
        /// <param name="attemptedValue">Optional attempted value that caused the error</param>
        /// <returns>A command response with BadRequest outcome entry</returns>
        public static new CommandResponse<TResult> BadRequest(string message, string propertyName = null, object attemptedValue = null)
            => new CommandResponse<TResult>(successful: false, OutcomeEntry.BadRequest(message, propertyName, attemptedValue));

        /// <summary>
        /// Creates an Unauthorized (401) error response with the specified message
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="propertyName">Optional property name associated with the error</param>
        /// <param name="attemptedValue">Optional attempted value that caused the error</param>
        /// <returns>A command response with Unauthorized outcome entry</returns>
        public static new CommandResponse<TResult> Unauthorized(string message, string propertyName = null, object attemptedValue = null)
            => new CommandResponse<TResult>(successful: false, OutcomeEntry.Unauthorized(message, propertyName, attemptedValue));

        /// <summary>
        /// Creates a Forbidden (403) error response with the specified message
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="propertyName">Optional property name associated with the error</param>
        /// <param name="attemptedValue">Optional attempted value that caused the error</param>
        /// <returns>A command response with Forbidden outcome entry</returns>
        public static new CommandResponse<TResult> Forbidden(string message, string propertyName = null, object attemptedValue = null)
            => new CommandResponse<TResult>(successful: false, OutcomeEntry.Forbidden(message, propertyName, attemptedValue));

        /// <summary>
        /// Creates a Conflict (409) error response with the specified message
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="propertyName">Optional property name associated with the error</param>
        /// <param name="attemptedValue">Optional attempted value that caused the error</param>
        /// <returns>A command response with Conflict outcome entry</returns>
        public static new CommandResponse<TResult> Conflict(string message, string propertyName = null, object attemptedValue = null)
            => new CommandResponse<TResult>(successful: false, OutcomeEntry.Conflict(message, propertyName, attemptedValue));

        /// <summary>
        /// Creates an InternalServerError (500) error response with the specified message
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="propertyName">Optional property name associated with the error</param>
        /// <param name="attemptedValue">Optional attempted value that caused the error</param>
        /// <returns>A command response with InternalServerError outcome entry</returns>
        public static new CommandResponse<TResult> InternalServerError(string message, string propertyName = null, object attemptedValue = null)
            => new CommandResponse<TResult>(successful: false, OutcomeEntry.InternalServerError(message, propertyName, attemptedValue));
    }
}
