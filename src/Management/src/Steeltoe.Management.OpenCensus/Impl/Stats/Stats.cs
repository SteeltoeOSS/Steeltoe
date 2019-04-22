using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    public class Stats 
    {
        private static Stats _stats = new Stats();

        internal Stats()
            : this(false)
        {

        }
        internal Stats(bool enabled)
        {
            if (enabled)
            {
                statsComponent = new StatsComponent();
            }
            else
            {
                statsComponent = NoopStats.NewNoopStatsComponent();
            }
        }


        private  IStatsComponent statsComponent = new StatsComponent();

        public static IStatsRecorder StatsRecorder
        {
            get
            {
                return _stats.statsComponent.StatsRecorder;
            }
        }
        public static IViewManager ViewManager
        {
            get
            {
                return _stats.statsComponent.ViewManager;
            }
        }
        public static StatsCollectionState State
        {
            get
            {
                return _stats.statsComponent.State;
            }
        }
    }
}
