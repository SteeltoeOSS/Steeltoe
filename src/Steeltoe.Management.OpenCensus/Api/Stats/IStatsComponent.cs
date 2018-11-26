using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    public interface IStatsComponent
    {
        IViewManager ViewManager { get; }
        IStatsRecorder StatsRecorder { get; }
        StatsCollectionState State { get; }
    }
}
