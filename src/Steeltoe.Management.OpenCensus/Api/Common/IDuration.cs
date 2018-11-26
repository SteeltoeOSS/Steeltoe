using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Common
{
    [Obsolete("Use OpenCensus project packages")]
    public interface IDuration : IComparable<IDuration>
    {
        long Seconds { get; }
        int Nanos { get; }
    }
}
