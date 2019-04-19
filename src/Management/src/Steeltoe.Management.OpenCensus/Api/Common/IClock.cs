using Steeltoe.Management.Census.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Common
{
    [Obsolete("Use OpenCensus project packages")]
    public interface IClock
    {
        ITimestamp Now { get; }
        long NowNanos { get; }
    }
}
