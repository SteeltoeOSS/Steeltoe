using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    internal sealed class NoopStatsComponent : StatsComponentBase
    {
        private readonly IViewManager viewManager = NoopStats.NewNoopViewManager();
        //private volatile bool isRead;

        public override IViewManager ViewManager
        {
            get
            {
                return viewManager;
            }
        }

        public override IStatsRecorder StatsRecorder
        {
            get
            {
                return NoopStats.NoopStatsRecorder;
            }
        }

        public override StatsCollectionState State
        {
            get
            {
                //isRead = true;
                return StatsCollectionState.DISABLED;
            }
            set
            {

            }
        }
    }
}
