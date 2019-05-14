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

using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public class LatencyBucketBoundaries : ISampledLatencyBucketBoundaries
    {
        public static readonly ISampledLatencyBucketBoundaries ZERO_MICROSx10 = new LatencyBucketBoundaries(0, 10000);
        public static readonly ISampledLatencyBucketBoundaries MICROSx10_MICROSx100 = new LatencyBucketBoundaries(10000, 100000);
        public static readonly ISampledLatencyBucketBoundaries MICROSx100_MILLIx1 = new LatencyBucketBoundaries(100000, 1000000);
        public static readonly ISampledLatencyBucketBoundaries MILLIx1_MILLIx10 = new LatencyBucketBoundaries(1000000, 10000000);
        public static readonly ISampledLatencyBucketBoundaries MILLIx10_MILLIx100 = new LatencyBucketBoundaries(10000000, 100000000);
        public static readonly ISampledLatencyBucketBoundaries MILLIx100_SECONDx1 = new LatencyBucketBoundaries(100000000, 1000000000);
        public static readonly ISampledLatencyBucketBoundaries SECONDx1_SECONDx10 = new LatencyBucketBoundaries(1000000000, 10000000000);
        public static readonly ISampledLatencyBucketBoundaries SECONDx10_SECONDx100 = new LatencyBucketBoundaries(10000000000, 100000000000);
        public static readonly ISampledLatencyBucketBoundaries SECONDx100_MAX = new LatencyBucketBoundaries(100000000000, long.MaxValue);

        public static IList<ISampledLatencyBucketBoundaries> Values = new List<ISampledLatencyBucketBoundaries>()
        {
            ZERO_MICROSx10, MICROSx10_MICROSx100, MICROSx100_MILLIx1, MILLIx1_MILLIx10, MILLIx10_MILLIx100, MILLIx100_SECONDx1, SECONDx1_SECONDx10, SECONDx10_SECONDx100, SECONDx100_MAX
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
