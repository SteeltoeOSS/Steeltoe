using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Internal
{
    [Obsolete("Use OpenCensus project packages")]
    public class SimpleEventQueue : IEventQueue
    {
        public void Enqueue(IEventQueueEntry entry)
        {
            entry.Process();
        }
    }
}
