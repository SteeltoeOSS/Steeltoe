using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public interface ITimedEvents<T>
    {
        IList<ITimedEvent<T>> Events { get; }
        int DroppedEventsCount { get; }
    }
}
