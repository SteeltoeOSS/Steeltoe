using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    public class StatsComponent : StatsComponentBase
    {

        // The StatsCollectionState shared between the StatsComponent, StatsRecorder and ViewManager.
        private readonly CurrentStatsState state = new CurrentStatsState();

        private readonly IViewManager viewManager;
        private readonly IStatsRecorder statsRecorder;

        public StatsComponent()
            :this(new SimpleEventQueue(), DateTimeOffsetClock.INSTANCE)
        {

        }
        public StatsComponent(IEventQueue queue, IClock clock)
        {
            StatsManager statsManager = new StatsManager(queue, clock, state);
            this.viewManager = new ViewManager(statsManager);
            this.statsRecorder = new StatsRecorder(statsManager);
        }

        public override IViewManager ViewManager
        {
            get { return viewManager; }
        }

        public override IStatsRecorder StatsRecorder
        {
            get { return statsRecorder; }
        }

        public override StatsCollectionState State
        {
            get { return state.Value; }
        }
    }
}
