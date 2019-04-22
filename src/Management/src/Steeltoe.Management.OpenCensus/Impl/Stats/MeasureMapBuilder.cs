using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Census.Stats.Measurements;
using Steeltoe.Management.Census.Stats.Measures;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    internal class MeasureMapBuilder
    {
        private IList<IMeasurement> measurements = new List<IMeasurement>();

        internal static MeasureMapBuilder Builder()
        {
            return new MeasureMapBuilder();
        }

        internal MeasureMapBuilder Put(IMeasureDouble measure, double value)
        {
            measurements.Add(MeasurementDouble.Create(measure, value));
            return this;
        }

        internal MeasureMapBuilder Put(IMeasureLong measure, long value)
        {
            measurements.Add(MeasurementLong.Create(measure, value));
            return this;
        }
        internal IList<IMeasurement> Build()
        {
            // Note: this makes adding measurements quadratic but is fastest for the sizes of
            // MeasureMapInternals that we should see. We may want to go to a strategy of sort/eliminate
            // for larger MeasureMapInternals.
            for (int i = measurements.Count - 1; i >= 0; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    if (measurements[i].Measure == measurements[j].Measure)
                    {
                        measurements.RemoveAt(j);
                        j--;
                    }
                }
            }
            return new List<IMeasurement>(measurements);
        }
    }
}
