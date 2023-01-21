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

        public CommandResponse()
        {
            OutcomeEntries = new List<IOutcomeEntry>();
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

        public CommandResponse(TResult result) : base()
        {
            Result = result;
        }
    }
}
