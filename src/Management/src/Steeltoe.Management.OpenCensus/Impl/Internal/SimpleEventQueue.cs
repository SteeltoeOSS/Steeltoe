using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Internal
{
    public class SimpleEventQueue : IEventQueue
    {
        public void Enqueue(IEventQueueEntry entry)
        {
            entry.Process();
        }
    }
}
