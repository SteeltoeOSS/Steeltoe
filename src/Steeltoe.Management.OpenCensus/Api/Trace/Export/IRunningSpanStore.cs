using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    public interface IRunningSpanStore
    {
        IRunningSpanStoreSummary Summary { get; }
        IList<ISpanData> GetRunningSpans(IRunningSpanStoreFilter filter);
        void OnStart(ISpan span);
        void OnEnd(ISpan span);
    }
}
