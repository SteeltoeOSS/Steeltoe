using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    internal class SpanBuilderOptions
    {
        internal IRandomGenerator RandomHandler { get; }
        internal IStartEndHandler StartEndHandler { get; }
        internal IClock Clock { get; }
        internal ITraceConfig TraceConfig { get; }

        internal SpanBuilderOptions(IRandomGenerator randomGenerator, IStartEndHandler startEndHandler, IClock clock, ITraceConfig traceConfig )
        {
            RandomHandler = randomGenerator;
            StartEndHandler = startEndHandler;
            Clock = clock;
            TraceConfig = traceConfig;
        }
    }
}
