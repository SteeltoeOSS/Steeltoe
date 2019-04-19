using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    internal sealed class NoopRunningSpanStore : RunningSpanStoreBase
    {
        private static readonly IRunningSpanStoreSummary EMPTY_SUMMARY =  RunningSpanStoreSummary.Create(new Dictionary<string, IRunningPerSpanNameSummary>());
        public override IRunningSpanStoreSummary Summary
        {
            get
            {
                return EMPTY_SUMMARY;
            }
        }

        public override IList<ISpanData> GetRunningSpans(IRunningSpanStoreFilter filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }
            return new List<ISpanData>();
        }

        public override void OnEnd(ISpan span)
        {
        }

        public override void OnStart(ISpan span)
        {
        }
    }
}
