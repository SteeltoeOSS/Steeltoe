using Steeltoe.Management.Census.Stats.Measures;
using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats.Measurements
{
    public sealed class MeasurementDouble : Measurement, IMeasurementDouble
    {
        public override IMeasure Measure { get; }
        public double Value { get; }

        internal MeasurementDouble(IMeasureDouble measure, double value)
        {
            if (measure == null)
            {
                throw new ArgumentNullException(nameof(measure));
            }
            this.Measure = measure;
            this.Value = value;
        }
        public static IMeasurementDouble Create(IMeasureDouble measure, double value)
        {
            return new MeasurementDouble(measure, value);
        }

        public override M Match<M>(Func<IMeasurementDouble, M> p0, Func<IMeasurementLong, M> p1, Func<IMeasurement, M> defaultFunction)
        {
            return p0.Invoke(this);
        }

        public override string ToString()
        {
            return "MeasurementDouble{"
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
            if (o is MeasurementDouble)
            {
                MeasurementDouble that = (MeasurementDouble)o;
                return (this.Measure.Equals(that.Measure))
                     && (DoubleUtil.ToInt64(this.Value) == DoubleUtil.ToInt64(that.Value));
            }
            return false;
        }

        public override int GetHashCode()
        {
            long h = 1;
            h *= 1000003;
            h ^= this.Measure.GetHashCode();
            h *= 1000003;
            h ^= (DoubleUtil.ToInt64(this.Value) >> 32) ^ DoubleUtil.ToInt64(this.Value);
            return (int)h;
        }

    }
}
