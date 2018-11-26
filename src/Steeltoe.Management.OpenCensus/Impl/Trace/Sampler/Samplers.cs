using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Sampler
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class Samplers
    {
        private static readonly ISampler ALWAYS_SAMPLE = new AlwaysSampleSampler();
        private static readonly ISampler NEVER_SAMPLE = new NeverSampleSampler();

        public static ISampler AlwaysSample
        {
            get
            {
                return ALWAYS_SAMPLE;
            }
        }
        public static ISampler NeverSample
        {
            get
            {
                return NEVER_SAMPLE;
            }
        }
        public static ISampler GetProbabilitySampler(double probability)
        {
            return ProbabilitySampler.Create(probability);
        }
    }
}
