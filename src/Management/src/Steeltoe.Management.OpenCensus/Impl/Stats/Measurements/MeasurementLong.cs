using Steeltoe.Management.Census.Stats.Measures;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats.Measurements
{
    public sealed class MeasurementLong : Measurement, IMeasurementLong
    {
        public override IMeasure Measure { get; }
        public long Value { get; }

        MeasurementLong(IMeasureLong measure, long value)
        {
            if (measure == null)
            {
                throw new ArgumentNullException(nameof(measure));
            }
            this.Measure = measure;
            this.Value = value;
        }

        public static IMeasurementLong Create(IMeasureLong measure, long value)
        {
            return new MeasurementLong(measure, value);
        }

        public override M Match<M>(Func<IMeasurementDouble, M> p0, Func<IMeasurementLong, M> p1, Func<IMeasurement, M> defaultFunction)
        {
            return p1.Invoke(this);
        }

        public override string ToString()
        {
            return "MeasurementLong{"
                + "measure=" + Measure + ", "
                + "value=" + Value
                + "}";
        }

        public override bool Equals(Object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is MeasurementLong)
            {
                MeasurementLong that = (MeasurementLong)o;
                return (this.Measure.Equals(that.Measure))
                     && (this.Value == that.Value);
            }
            return false;
        }

        public override int GetHashCode()
        {
            long h = 1;
            h *= 1000003;
            h ^= this.Measure.GetHashCode();
            h *= 1000003;
            h ^= (this.Value >> 32) ^ this.Value;
            return (int)h;
        }

  
    }
}
