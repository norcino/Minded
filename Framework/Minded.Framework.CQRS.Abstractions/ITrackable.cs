using System;

namespace Minded.Framework.CQRS.Abstractions
{
    /// <summary>
    /// The trackable interface can be used to describe commands leveraging the tracking id
    /// </summary>
    public interface ITrackable
    {
        Guid TrackingId { get; }
    }
}
