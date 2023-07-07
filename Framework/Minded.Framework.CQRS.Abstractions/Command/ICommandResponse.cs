using System.Collections.Generic;
using Minded.Framework.CQRS.Abstractions;

namespace Minded.Framework.CQRS.Command
{
    /// <summary>
    /// Default command response whit information about the success of the command and optionally a list of <see cref="OutcomeEntry"/>.
    /// </summary>
    public interface ICommandResponse
    {
        bool Successful { get; set; }

        List<IOutcomeEntry> OutcomeEntries { get; set; }
    }
    
    /// <summary>
    /// Extended version of <see cref="ICommandResponse"/> with a generic type Result, containing possible Command output.
    /// </summary>
    /// <typeparam name="TResult">Command result</typeparam>
    public interface ICommandResponse<out TResult> : ICommandResponse
    {
        TResult Result { get; }
    }
}
