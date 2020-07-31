// <copyright file="MutableMean.cs" company="OpenCensus Authors">
// Copyright 2018, OpenCensus Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenCensus.Stats
{
    using System;

    internal sealed class MutableMean : MutableAggregation
    {
        internal MutableMean()
        {
        }

        internal double Sum { get; set; } = 0.0;

        internal long Count { get; set; } = 0;

        internal double Mean
        {
            get
            {
                return Count == 0 ? 0 : Sum / Count;
            }
        }

        internal double Min { get; set; } = double.MaxValue;

        internal double Max { get; set; } = double.MinValue;

        internal static MutableMean Create()
        {
            return new MutableMean();
        }

        internal override void Add(double value)
        {
            Count++;
            Sum += value;
            if (value < Min)
            {
                Min = value;
            }

            if (value > Max)
            {
                Max = value;
            }
        }

        internal override void Combine(MutableAggregation other, double fraction)
        {
            if (!(other is MutableMean mutable))
            {
                throw new ArgumentException("MutableMean expected.");
            }

            var result = fraction * mutable.Count;
            var rounded = (long)Math.Round(result);
            Count += rounded;

            Sum += mutable.Sum * fraction;

            if (mutable.Min < Min)
            {
                Min = mutable.Min;
            }

            if (mutable.Max > Max)
            {
                Max = mutable.Max;
            }
        }

        internal override T Match<T>(Func<MutableSum, T> p0, Func<MutableCount, T> p1, Func<MutableMean, T> p2, Func<MutableDistribution, T> p3, Func<MutableLastValue, T> p4)
        {
            return p2.Invoke(this);
        }
    }
}
