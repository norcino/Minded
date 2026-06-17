using System.Collections.Generic;

namespace Minded.Framework.CQRS.Abstractions
{
    /// <summary>
    /// Base response interface shared by <see cref="Minded.Framework.CQRS.Command.ICommandResponse"/> and
    /// <see cref="Minded.Framework.CQRS.Query.IQueryResponse{TResult}"/>.
    /// Carries the outcome of a command or query execution, including a success flag and a list of detail entries.
    /// </summary>
    public interface IMessageResponse
    {
        /// <summary>
        /// Indicates whether the command or query executed successfully.
        /// A value of <c>false</c> does not necessarily mean an exception occurred;
        /// business-rule failures are also expressed as unsuccessful responses via <see cref="OutcomeEntries"/>.
        /// </summary>
        bool Successful { get; set; }

        /// <summary>
        /// Collection of <see cref="IOutcomeEntry"/> items produced during processing.
        /// May contain validation errors, warnings, or informational messages regardless of <see cref="Successful"/>.
        /// </summary>
        List<IOutcomeEntry> OutcomeEntries { get; set; }
    }
}
