using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    public interface ILinks
    {
        IList<ILink> Links { get; }
        int DroppedLinksCount { get; }
    }
}
