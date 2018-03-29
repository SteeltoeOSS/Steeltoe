using Steeltoe.Management.Census.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Common
{
    public interface IClock
    {
        ITimestamp Now { get; }
        long NowNanos { get; }
    }
}
