using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    public class LatencyBucketBoundaries : ISampledLatencyBucketBoundaries
    {
        public static readonly ISampledLatencyBucketBoundaries ZERO_MICROSx10 = new LatencyBucketBoundaries(0, 10000);
        public static readonly ISampledLatencyBucketBoundaries MICROSx10_MICROSx100 = new LatencyBucketBoundaries(10000, 100000);
        public static readonly ISampledLatencyBucketBoundaries MICROSx100_MILLIx1 = new LatencyBucketBoundaries(100000, 1000000);
        public static readonly ISampledLatencyBucketBoundaries MILLIx1_MILLIx10 = new LatencyBucketBoundaries(1000000, 10000000);
        public static readonly ISampledLatencyBucketBoundaries MILLIx10_MILLIx100 = new LatencyBucketBoundaries(10000000, 100000000);
        public static readonly ISampledLatencyBucketBoundaries MILLIx100_SECONDx1 = new LatencyBucketBoundaries(100000000, 1000000000);
        public static readonly ISampledLatencyBucketBoundaries SECONDx1_SECONDx10 = new LatencyBucketBoundaries(1000000000,  10000000000);
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
