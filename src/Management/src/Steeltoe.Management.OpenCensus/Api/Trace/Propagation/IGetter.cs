using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Propagation
{
    public interface IGetter<C>
    {
        string Get(C carrier, string key);
    }
}
