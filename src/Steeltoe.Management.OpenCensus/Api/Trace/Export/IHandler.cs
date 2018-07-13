using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    public interface IHandler
    {
        void Export(IList<ISpanData> spanDataList);
    }
}
