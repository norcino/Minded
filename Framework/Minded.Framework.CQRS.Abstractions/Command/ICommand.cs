using System;

namespace Minded.Framework.CQRS.Command
{
    public interface ICommand
    {
        Guid TrackingId { get; }
    }

    public interface ICommand<TResult> : ICommand
    {
        TResult Result { get; }
    }
}
