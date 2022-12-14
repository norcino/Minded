using Minded.Extensions.Validation;
using System.Collections.Generic;

namespace Minded.Framework.CQRS.Command
{
    public class CommandResponse : ICommandResponse
    {
        public bool Successful { get; set; }

        public List<IValidationEntry> ValidationEntries { get; set; }

        public CommandResponse()
        {
            ValidationEntries = new List<IValidationEntry>();
        }
    }

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
