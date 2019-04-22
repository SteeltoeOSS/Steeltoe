using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    internal sealed class NoopStatsRecorder : StatsRecorderBase
    {
        internal static readonly IStatsRecorder INSTANCE = new NoopStatsRecorder();

    public override IMeasureMap NewMeasureMap()
        {
            return NoopStats.NoopMeasureMap;
        }
    }
}
