using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Sampler
{
    [Obsolete("Use OpenCensus project packages")]
    internal class AlwaysSampleSampler : ISampler
    {
        internal AlwaysSampleSampler() { }

        public string Description
        {
            get
            {
                return ToString();
            }
        }

        public bool ShouldSample(ISpanContext parentContext, bool hasRemoteParent, ITraceId traceId, ISpanId spanId, string name, IList<ISpan> parentLinks)
        {
            return true;
        }
        public override string ToString()
        {
            return "AlwaysSampleSampler";
        }
    }
}
