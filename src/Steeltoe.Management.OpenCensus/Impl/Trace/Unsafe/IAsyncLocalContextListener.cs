using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Unsafe
{
    public interface IAsyncLocalContextListener
    {
        void ContextChanged(ISpan oldSpan, ISpan newSapn, bool threadContextSwitch);
    }
}
