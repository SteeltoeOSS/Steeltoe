using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class RunningSpanStoreBase : IRunningSpanStore
    {
        private static readonly IRunningSpanStore NOOP_RUNNING_SPAN_STORE = new NoopRunningSpanStore();

        internal static IRunningSpanStore NoopRunningSpanStore
        {
            get
            {
                return NOOP_RUNNING_SPAN_STORE;
            }
        }
        protected RunningSpanStoreBase() { }

        public abstract IRunningSpanStoreSummary Summary { get; }
        public abstract IList<ISpanData> GetRunningSpans(IRunningSpanStoreFilter filter);
        public abstract void OnEnd(ISpan span);
        public abstract void OnStart(ISpan span);
    }
}
