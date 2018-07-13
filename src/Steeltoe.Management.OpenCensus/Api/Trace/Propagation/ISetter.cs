using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Propagation
{
    public interface ISetter<C>
    {
        void Put(C carrier, string key, string value);
    }
}
