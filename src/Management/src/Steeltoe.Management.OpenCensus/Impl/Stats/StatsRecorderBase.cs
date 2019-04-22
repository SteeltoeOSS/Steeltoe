using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    public abstract class StatsRecorderBase : IStatsRecorder
    {
        public abstract IMeasureMap NewMeasureMap();
    }
}
