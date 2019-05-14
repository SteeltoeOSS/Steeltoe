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

using Steeltoe.Management.Census.Utils;
using System;

namespace Steeltoe.Management.Census.Stats.Aggregations
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class SumDataDouble : AggregationData, ISumDataDouble
    {
        public double Sum { get; }

        internal SumDataDouble(double sum)
        {
            this.Sum = sum;
        }

        public static ISumDataDouble Create(double sum)
        {
            return new SumDataDouble(sum);
        }

        public override M Match<M>(
            Func<ISumDataDouble, M> p0,
            Func<ISumDataLong, M> p1,
            Func<ICountData, M> p2,
            Func<IMeanData, M> p3,
            Func<IDistributionData, M> p4,
            Func<ILastValueDataDouble, M> p5,
            Func<ILastValueDataLong, M> p6,
            Func<IAggregationData, M> defaultFunction)
        {
            return p0.Invoke(this);
        }

        public override string ToString()
        {
            return "SumDataDouble{"
                + "sum=" + Sum
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is SumDataDouble)
            {
                SumDataDouble that = (SumDataDouble)o;
                return DoubleUtil.ToInt64(this.Sum) == DoubleUtil.ToInt64(that.Sum);
            }

            return false;
        }

        public override int GetHashCode()
        {
            long h = 1;
            h *= 1000003;
            h ^= (DoubleUtil.ToInt64(this.Sum) >> 32) ^ DoubleUtil.ToInt64(this.Sum);
            return (int)h;
        }
    }
}
