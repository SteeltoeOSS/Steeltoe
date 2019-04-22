using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Internal;

namespace Steeltoe.Management.Census.Trace
{
    public sealed class Tracer : TracerBase
    {

        private readonly SpanBuilderOptions spanBuilderOptions;

        public Tracer(IRandomGenerator randomGenerator, IStartEndHandler startEndHandler, IClock clock, ITraceConfig traceConfig)
        {
            spanBuilderOptions = new SpanBuilderOptions(randomGenerator, startEndHandler, clock, traceConfig);
        }

        public override ISpanBuilder SpanBuilderWithExplicitParent(string spanName, ISpan parent = null)
        {
            return Trace.SpanBuilder.CreateWithParent(spanName, parent, spanBuilderOptions);
        }

        public override ISpanBuilder SpanBuilderWithRemoteParent(string spanName, ISpanContext remoteParentSpanContext = null)
        {
            return Trace.SpanBuilder.CreateWithRemoteParent(spanName, remoteParentSpanContext, spanBuilderOptions);
        }
    }
}
