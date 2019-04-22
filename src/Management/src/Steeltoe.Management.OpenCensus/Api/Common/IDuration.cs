using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Common
{
    public interface IDuration : IComparable<IDuration>
    {
        long Seconds { get; }
        int Nanos { get; }
    }
}
