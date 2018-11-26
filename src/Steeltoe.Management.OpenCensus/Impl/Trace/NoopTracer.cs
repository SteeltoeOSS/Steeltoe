

using System;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class NoopTracer : TracerBase, ITracer
    {
        public override ISpanBuilder SpanBuilderWithExplicitParent(string spanName, ISpan parent)
        {
            return NoopSpanBuilder.CreateWithParent(spanName, parent);
        }


        public override ISpanBuilder SpanBuilderWithRemoteParent(string spanName, ISpanContext remoteParentSpanContext)
        {
            return NoopSpanBuilder.CreateWithRemoteParent(spanName, remoteParentSpanContext);
        }

        internal NoopTracer() { }
    }
}
