using System;

namespace Minded.Framework.CQRS.Query
{
    public interface IQuery
    {
        Guid TrackingId { get; }
    }

    public interface IQuery<TResult> : IQuery
    {       
    }
}
