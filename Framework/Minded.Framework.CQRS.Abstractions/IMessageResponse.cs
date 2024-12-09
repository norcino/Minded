using System.Collections.Generic;

namespace Minded.Framework.CQRS.Abstractions
{
    public interface IMessageResponse
    {
        bool Successful { get; set; }

        List<IOutcomeEntry> OutcomeEntries { get; set; }
    }
}
