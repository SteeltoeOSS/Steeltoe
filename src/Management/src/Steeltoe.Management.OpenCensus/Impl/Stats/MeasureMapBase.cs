using Steeltoe.Management.Census.Stats.Measures;
using Steeltoe.Management.Census.Tags;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class MeasureMapBase : IMeasureMap
    {

        public abstract IMeasureMap Put(IMeasureDouble measure, double value);


        public abstract IMeasureMap Put(IMeasureLong measure, long value);

  
        public abstract void Record();

 
        public abstract void Record(ITagContext tags);
    }
}
