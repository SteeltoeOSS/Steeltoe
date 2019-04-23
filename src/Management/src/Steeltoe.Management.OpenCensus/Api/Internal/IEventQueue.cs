using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Internal
{
    [Obsolete("Use OpenCensus project packages")]
    public interface IEventQueue
    {
        void Enqueue(IEventQueueEntry entry);
    }
}
