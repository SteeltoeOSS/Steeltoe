using Steeltoe.Management.Census.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public interface ISpanData
    {
        ISpanContext Context { get; }
        ISpanId ParentSpanId { get; }
        bool? HasRemoteParent { get; }
        string Name { get; }
        ITimestamp StartTimestamp { get; }
        IAttributes Attributes { get; }
        ITimedEvents<IAnnotation> Annotations { get; }
        ITimedEvents<IMessageEvent> MessageEvents { get; }
        ILinks Links { get; }
        int? ChildSpanCount { get; }
        Status Status { get; }
        ITimestamp EndTimestamp { get; }
    }
}
