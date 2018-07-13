using Steeltoe.Management.Census.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Propagation
{
    internal class NoopTextFormat : TextFormatBase
    {
        internal NoopTextFormat() { }

        public override IList<string> Fields
        {
            get { return new List<string>(); }
        }

        public override void Inject<C>(ISpanContext spanContext, C carrier, ISetter<C> setter)
        {
            if (spanContext == null)
            {
                throw new ArgumentNullException(nameof(spanContext));
            }
            if (carrier == null)
            {
                throw new ArgumentNullException(nameof(carrier));
            }
            if (setter == null)
            {
                throw new ArgumentNullException(nameof(setter));
            }
    
        }

        public override ISpanContext Extract<C>(C carrier, IGetter<C> getter)
        {

            if (carrier == null)
            {
                throw new ArgumentNullException(nameof(carrier));
            }
            if (getter == null)
            {
                throw new ArgumentNullException(nameof(getter));
            }
            return SpanContext.INVALID;
        }
    }
}
