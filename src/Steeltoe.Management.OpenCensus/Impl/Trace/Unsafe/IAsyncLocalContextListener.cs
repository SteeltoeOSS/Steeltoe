using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Unsafe
{
    [Obsolete("Use OpenCensus project packages")]
    public interface IAsyncLocalContextListener
    {
        void ContextChanged(ISpan oldSpan, ISpan newSapn, bool threadContextSwitch);
    }
}
