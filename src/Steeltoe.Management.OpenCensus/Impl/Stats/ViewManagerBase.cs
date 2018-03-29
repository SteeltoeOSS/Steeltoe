using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    public abstract class ViewManagerBase : IViewManager
    {
        public abstract ISet<IView> AllExportedViews { get; }

        public abstract IViewData GetView(IViewName view);
        public abstract void RegisterView(IView view);
    }
}
