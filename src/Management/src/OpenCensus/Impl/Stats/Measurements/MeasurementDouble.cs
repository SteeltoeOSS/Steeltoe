// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Management.Census.Stats.Measures;
using Steeltoe.Management.Census.Utils;
using System;

namespace Steeltoe.Management.Census.Stats.Measurements
{
    [Obsolete("Use OpenCensus project packages")]
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

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is MeasurementDouble)
            {
                MeasurementDouble that = (MeasurementDouble)o;
                return this.Measure.Equals(that.Measure)
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
