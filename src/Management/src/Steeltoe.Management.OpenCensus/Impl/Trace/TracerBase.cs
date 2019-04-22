using Steeltoe.Management.Census.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    public abstract class TracerBase : ITracer
    {
        private static readonly NoopTracer noopTracer = new NoopTracer();
        internal static NoopTracer NoopTracer
        {
            get
            {
                return noopTracer;
            }
        }

        public ISpan CurrentSpan
        {
            get
            {
                ISpan currentSpan = CurrentSpanUtils.CurrentSpan;
                return currentSpan != null ? currentSpan : BlankSpan.INSTANCE;
            }
        }
        public IScope WithSpan(ISpan span)
        {
            if (span == null)
            {
                throw new ArgumentNullException(nameof(span));
            }
            return CurrentSpanUtils.WithSpan(span, false);
        }

        //public final Runnable withSpan(Span span, Runnable runnable)
        //public final <C> Callable<C> withSpan(Span span, final Callable<C> callable)

        public ISpanBuilder SpanBuilder(string spanName)
        {
            return SpanBuilderWithExplicitParent(spanName, CurrentSpanUtils.CurrentSpan);
        }

        public abstract ISpanBuilder SpanBuilderWithExplicitParent(string spanName, ISpan parent = null);

        public abstract ISpanBuilder SpanBuilderWithRemoteParent(string spanName, ISpanContext remoteParentSpanContext = null);

    }

}
