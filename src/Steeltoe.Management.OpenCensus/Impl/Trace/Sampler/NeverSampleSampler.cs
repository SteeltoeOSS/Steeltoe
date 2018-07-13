using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Sampler
{
    internal sealed class NeverSampleSampler : ISampler
    {
        internal NeverSampleSampler() { }

        public string Description
        {
            get
            {
                return ToString();
            }
        }

        public bool ShouldSample(ISpanContext parentContext, bool hasRemoteParent, ITraceId traceId, ISpanId spanId, string name, IList<ISpan> parentLinks)
        {
            return false;
        }
        public override string ToString()
        {
            return "NeverSampleSampler";
        }
    }
}
