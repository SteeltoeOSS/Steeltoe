

using Steeltoe.Management.Census.Common;
using System;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public interface ITimedEvent<T>
    {
        ITimestamp Timestamp { get; }
        T Event { get; }
    }
}
