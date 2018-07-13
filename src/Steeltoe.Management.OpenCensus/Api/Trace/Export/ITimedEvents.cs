using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    public interface ITimedEvents<T>
    {
        IList<ITimedEvent<T>> Events { get; }
        int DroppedEventsCount { get; }
    }
}
