using System.Collections.Generic;
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

        public CommandResponse() => OutcomeEntries = new List<IOutcomeEntry>();

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
    }
}
