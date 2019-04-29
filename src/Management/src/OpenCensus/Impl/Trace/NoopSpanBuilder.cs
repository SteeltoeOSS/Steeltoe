using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public class NoopSpanBuilder : SpanBuilderBase
    {
        internal static ISpanBuilder CreateWithParent(string spanName, ISpan parent = null)
        {
            return new NoopSpanBuilder(spanName);
        }

        internal static ISpanBuilder CreateWithRemoteParent(string spanName, ISpanContext remoteParentSpanContext = null)
        {
            return new NoopSpanBuilder(spanName);
        }

        public override ISpan StartSpan()
        {
            return BlankSpan.INSTANCE;
        }

        public override ISpanBuilder SetSampler(ISampler sampler)
        {
            return this;
        }

        public override ISpanBuilder SetParentLinks(IList<ISpan> parentLinks)
        {
            return this;
        }

        public override ISpanBuilder SetRecordEvents(bool recordEvents)
        {
            return this;
        }

        private NoopSpanBuilder(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
        }
    }
}
