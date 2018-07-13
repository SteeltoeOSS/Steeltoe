using Steeltoe.Management.Census.Stats.Measures;
using Steeltoe.Management.Census.Tags;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    public interface IMeasureMap
    {
        IMeasureMap Put(IMeasureDouble measure, double value);


        IMeasureMap Put(IMeasureLong measure, long value);


        void Record();


        void Record(ITagContext tags);
    }
}
