using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    [Flags]
    public enum SpanOptions
    {
        NONE = 0x0,
        RECORD_EVENTS = 0x1
    }
}
