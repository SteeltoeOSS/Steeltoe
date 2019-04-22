using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Common
{
    [Obsolete("Use OpenCensus project packages")]
    public interface ITimestamp : IComparable<ITimestamp>
    {
        long Seconds { get; }
        int Nanos { get; }
        ITimestamp AddNanos(long nanosToAdd);
        ITimestamp AddDuration(IDuration duration);
        IDuration SubtractTimestamp(ITimestamp timestamp);

    }
}
