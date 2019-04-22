using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    internal sealed class NoopStats
    {
        private NoopStats() { }

  
        internal static IStatsComponent NewNoopStatsComponent()
        {
            return new NoopStatsComponent();
        }

   
        internal static IStatsRecorder NoopStatsRecorder
        {
            get
            {
                return Census.Stats.NoopStatsRecorder.INSTANCE;
            }
        }

        internal static IMeasureMap NoopMeasureMap
        {
            get
            {
                return Census.Stats.NoopMeasureMap.INSTANCE;
            }
        }
        internal static IViewManager NewNoopViewManager()
        {
            return new NoopViewManager();
        }
    }
}
