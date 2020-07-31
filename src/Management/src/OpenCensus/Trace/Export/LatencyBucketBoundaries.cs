// <copyright file="LatencyBucketBoundaries.cs" company="OpenCensus Authors">
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

namespace OpenCensus.Trace.Export
{
    using System.Collections.Generic;

    public class LatencyBucketBoundaries : ISampledLatencyBucketBoundaries
    {
        public static readonly ISampledLatencyBucketBoundaries ZeroMicrosx10 = new LatencyBucketBoundaries(0, 10000);
        public static readonly ISampledLatencyBucketBoundaries Microsx10Microsx100 = new LatencyBucketBoundaries(10000, 100000);
        public static readonly ISampledLatencyBucketBoundaries Microsx100Millix1 = new LatencyBucketBoundaries(100000, 1000000);
        public static readonly ISampledLatencyBucketBoundaries Millix1Millix10 = new LatencyBucketBoundaries(1000000, 10000000);
        public static readonly ISampledLatencyBucketBoundaries Millix10Millix100 = new LatencyBucketBoundaries(10000000, 100000000);
        public static readonly ISampledLatencyBucketBoundaries Millix100Secondx1 = new LatencyBucketBoundaries(100000000, 1000000000);
        public static readonly ISampledLatencyBucketBoundaries Secondx1Secondx10 = new LatencyBucketBoundaries(1000000000,  10000000000);
        public static readonly ISampledLatencyBucketBoundaries Secondx10Secondx100 = new LatencyBucketBoundaries(10000000000, 100000000000);
        public static readonly ISampledLatencyBucketBoundaries Secondx100Max = new LatencyBucketBoundaries(100000000000, long.MaxValue);

        public static IList<ISampledLatencyBucketBoundaries> Values = new List<ISampledLatencyBucketBoundaries>()
        {
            ZeroMicrosx10, Microsx10Microsx100, Microsx100Millix1, Millix1Millix10, Millix10Millix100, Millix100Secondx1, Secondx1Secondx10, Secondx10Secondx100, Secondx100Max,
        };

        internal LatencyBucketBoundaries(long latencyLowerNs, long latencyUpperNs)
        {
            LatencyLowerNs = latencyLowerNs;
            LatencyUpperNs = latencyUpperNs;
        }

        public long LatencyLowerNs { get; }

        public long LatencyUpperNs { get; }
    }
}
