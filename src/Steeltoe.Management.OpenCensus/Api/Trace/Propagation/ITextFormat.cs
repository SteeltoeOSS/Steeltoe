using Steeltoe.Management.Census.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Propagation
{
    public interface ITextFormat
    {
        IList<string> Fields { get; }
        void Inject<C>(ISpanContext spanContext, C carrier, ISetter<C> setter);
        ISpanContext Extract<C>(C carrier, IGetter<C> getter);
    }
}
