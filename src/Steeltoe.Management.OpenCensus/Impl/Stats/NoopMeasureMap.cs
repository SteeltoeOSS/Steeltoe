using Steeltoe.Management.Census.Stats.Measures;
using Steeltoe.Management.Census.Tags;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    internal sealed class NoopMeasureMap : MeasureMapBase
    {
        internal static readonly NoopMeasureMap INSTANCE = new NoopMeasureMap();

        public override IMeasureMap Put(IMeasureDouble measure, double value)
        {
            return this;
        }

        public override IMeasureMap Put(IMeasureLong measure, long value)
        {
            return this;
        }

        public override void Record() { }

        public override void Record(ITagContext tags)
        {
           if (tags == null)
            {
                throw new ArgumentNullException(nameof(tags));
            }
        }
    }
}
