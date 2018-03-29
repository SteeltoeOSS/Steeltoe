using Steeltoe.Management.Census.Internal;
using Steeltoe.Management.Census.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Utils
{
    public class DefaultEventQueue : IEventQueue
    {
        public void Enqueue(IEventQueueEntry entry)
        {
            throw new NotImplementedException();
        }
    }
}
