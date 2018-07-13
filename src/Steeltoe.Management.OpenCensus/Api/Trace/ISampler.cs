using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    public interface ISampler
    {
        string Description { get; }
        bool ShouldSample(ISpanContext parentContext, bool hasRemoteParent, ITraceId traceId, ISpanId spanId, string name, IList<ISpan> parentLinks);

    }
}
