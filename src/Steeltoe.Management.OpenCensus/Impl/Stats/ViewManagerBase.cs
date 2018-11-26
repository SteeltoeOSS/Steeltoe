using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class ViewManagerBase : IViewManager
    {
        public abstract ISet<IView> AllExportedViews { get; }

        public abstract IViewData GetView(IViewName view);
        public abstract void RegisterView(IView view);
    }
}
