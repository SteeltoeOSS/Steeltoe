using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Sampler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.CensusBase.Trace.Config.Test
{
    public class TraceParamsTest
    {
        [Fact]
        public void DefaultTraceParams()
        {
            Assert.Equal(Samplers.GetProbabilitySampler(1e-4), TraceParams.DEFAULT.Sampler);
            Assert.Equal(32, TraceParams.DEFAULT.MaxNumberOfAttributes);
            Assert.Equal(32, TraceParams.DEFAULT.MaxNumberOfAnnotations);
            Assert.Equal(128, TraceParams.DEFAULT.MaxNumberOfMessageEvents);
            Assert.Equal(128, TraceParams.DEFAULT.MaxNumberOfLinks);
        }

        [Fact]
        public void UpdateTraceParams_NullSampler()
        {
            Assert.Throws<ArgumentNullException>(() => TraceParams.DEFAULT.ToBuilder().SetSampler(null));
        }

        [Fact]
        public void UpdateTraceParams_NonPositiveMaxNumberOfAttributes()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TraceParams.DEFAULT.ToBuilder().SetMaxNumberOfAttributes(0).Build());
        }

        [Fact]
        public void UpdateTraceParams_NonPositiveMaxNumberOfAnnotations()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TraceParams.DEFAULT.ToBuilder().SetMaxNumberOfAnnotations(0).Build()); 
        }


        [Fact]
        public void updateTraceParams_NonPositiveMaxNumberOfMessageEvents()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TraceParams.DEFAULT.ToBuilder().SetMaxNumberOfMessageEvents(0).Build());
        }

        [Fact]
        public void updateTraceParams_NonPositiveMaxNumberOfLinks()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TraceParams.DEFAULT.ToBuilder().SetMaxNumberOfLinks(0).Build());
        }

        [Fact]
        public void UpdateTraceParams_All()
        {
            TraceParams traceParams =
              TraceParams.DEFAULT
                  .ToBuilder()
                  .SetSampler(Samplers.AlwaysSample)
                  .SetMaxNumberOfAttributes(8)
                  .SetMaxNumberOfAnnotations(9)
                  .SetMaxNumberOfMessageEvents(10)
                  .SetMaxNumberOfLinks(11)
                  .Build();

            Assert.Equal(Samplers.AlwaysSample, traceParams.Sampler);
            Assert.Equal(8, traceParams.MaxNumberOfAttributes);
            Assert.Equal(9, traceParams.MaxNumberOfAnnotations);
            // test maxNumberOfNetworkEvent can be set via maxNumberOfMessageEvent
            Assert.Equal(10, traceParams.MaxNumberOfMessageEvents);
            Assert.Equal(11, traceParams.MaxNumberOfLinks);
        }
    }
}
