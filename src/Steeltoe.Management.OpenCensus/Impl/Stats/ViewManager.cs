using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class ViewManager : ViewManagerBase
    {
        private readonly StatsManager statsManager;

        internal ViewManager(StatsManager statsManager)
        {
            this.statsManager = statsManager;
        }

        public override void RegisterView(IView view)
        {
            statsManager.RegisterView(view);
        }


        public override IViewData GetView(IViewName viewName)
        {
            return statsManager.GetView(viewName);
        }


        public override ISet<IView> AllExportedViews
        {
            get
            {
                return statsManager.ExportedViews;
            }
        }

        internal void ClearStats()
        {
            statsManager.ClearStats();
        }

        internal void ResumeStatsCollection()
        {
            statsManager.ResumeStatsCollection();
        }
    }
}
