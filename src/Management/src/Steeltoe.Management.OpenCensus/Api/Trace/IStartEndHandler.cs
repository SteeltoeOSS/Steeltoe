using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public interface IStartEndHandler
    {
        void OnStart(SpanBase span);

        void OnEnd(SpanBase span);
    }
}
