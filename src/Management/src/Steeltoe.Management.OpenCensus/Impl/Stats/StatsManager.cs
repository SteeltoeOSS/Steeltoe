using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Internal;
using Steeltoe.Management.Census.Tags;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    internal sealed class StatsManager
    {
        private readonly IEventQueue queue;

        // clock used throughout the stats implementation
        private readonly IClock clock;

        private readonly CurrentStatsState state;
        private readonly MeasureToViewMap measureToViewMap = new MeasureToViewMap();

        internal StatsManager(IEventQueue queue, IClock clock, CurrentStatsState state)
        {
            if (queue == null)
            {
                throw new ArgumentNullException(nameof(queue));
            }
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            this.queue = queue;
            this.clock = clock;
            this.state = state;
        }

        internal void RegisterView(IView view)
        {
            measureToViewMap.RegisterView(view, clock);
        }

        internal IViewData GetView(IViewName viewName)
        {
            return measureToViewMap.GetView(viewName, clock, state.Internal);
        }

        internal ISet<IView> ExportedViews
        {
            get
            {
                return measureToViewMap.ExportedViews;
            }
        }

        internal void Record(ITagContext tags, IList<IMeasurement> measurementValues)
        {
            // TODO(songya): consider exposing No-op MeasureMap and use it when stats state is DISABLED, so
            // that we don't need to create actual MeasureMapImpl.
            if (state.Internal == StatsCollectionState.ENABLED)
            {
                queue.Enqueue(new StatsEvent(this, tags, measurementValues));
            }
        }

        internal void ClearStats()
        {
            measureToViewMap.ClearStats();
        }

        internal void ResumeStatsCollection()
        {
            measureToViewMap.ResumeStatsCollection(clock.Now);
        }

        private class StatsEvent : IEventQueueEntry
        {
            private readonly ITagContext tags;
            private readonly IList<IMeasurement> stats;
            private readonly StatsManager statsManager;

            public StatsEvent(StatsManager statsManager, ITagContext tags, IList<IMeasurement> stats)
            {
                this.statsManager = statsManager;
                this.tags = tags;
                this.stats = stats;
            }

            public void Process()
            {
                // Add Timestamp to value after it went through the DisruptorQueue.
                statsManager.measureToViewMap.Record(tags, stats, statsManager.clock.Now);
            }
        }

}
}
