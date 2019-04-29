using System;
using System.Collections.Generic;
using System.Text;
using Steeltoe.Management.Census.Trace;

namespace Steeltoe.Management.Census.Trace.Propagation
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class TextFormatBase : ITextFormat
    {
        private static readonly NoopTextFormat NOOP_TEXT_FORMAT = new NoopTextFormat();

        internal static ITextFormat NoopTextFormat
        {
            get
            {
                return NOOP_TEXT_FORMAT;
            }
        }
        public abstract IList<string> Fields { get; }

        public abstract ISpanContext Extract<C>(C carrier, IGetter<C> getter);

        public abstract void Inject<C>(ISpanContext spanContext, C carrier, ISetter<C> setter);

    }
}
