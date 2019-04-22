using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    public interface IStatsRecorder
    {
        IMeasureMap NewMeasureMap();
    }
}
