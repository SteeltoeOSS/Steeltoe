using Steeltoe.Management.Census.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    public abstract class SpanBuilderBase : ISpanBuilder
    {
        public abstract ISpanBuilder SetSampler(ISampler sampler);
        public abstract ISpanBuilder SetParentLinks(IList<ISpan> parentLinks);
        public abstract ISpanBuilder SetRecordEvents(bool recordEvents);
        public abstract ISpan StartSpan();
        public IScope StartScopedSpan()
        {
            return CurrentSpanUtils.WithSpan(StartSpan(), true);
        }
        //public void StartSpanAndRun(Runnable runnable)
        //{
        //    Span span = startSpan();
        //    CurrentSpanUtils.withSpan(span, /* endSpan= */ true, runnable).run();
        //}
        //public final<V> V startSpanAndCall(Callable<V> callable) throws Exception
        //{
        //    final Span span = startSpan();
        //    return CurrentSpanUtils.withSpan(span, /* endSpan= */ true, callable).call();
        //}
    }

}
