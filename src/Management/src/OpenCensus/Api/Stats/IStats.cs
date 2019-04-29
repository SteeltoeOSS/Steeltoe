using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    public interface IStats
    {
        IStatsRecorder StatsRecorder { get; }
        IViewManager ViewManager { get; }
        StatsCollectionState State { get; set; }
    }
}
