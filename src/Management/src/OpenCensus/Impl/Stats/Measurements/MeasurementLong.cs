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
using System;

namespace Steeltoe.Management.Census.Stats.Measurements
{
    [Obsolete("Use OpenCensus project packages")]
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

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is MeasurementLong)
            {
                MeasurementLong that = (MeasurementLong)o;
                return this.Measure.Equals(that.Measure)
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
