using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    public abstract class StatsComponentBase : IStatsComponent
    {
        public abstract IViewManager ViewManager { get; }
        public abstract IStatsRecorder StatsRecorder { get; }
        public abstract StatsCollectionState State { get; }
    }
}
