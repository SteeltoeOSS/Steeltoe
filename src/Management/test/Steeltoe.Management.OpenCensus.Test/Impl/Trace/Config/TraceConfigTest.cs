using Steeltoe.Management.Census.Trace.Sampler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Config.Test
{
    public class TraceConfigTest
    {
        private readonly TraceConfig traceConfig = new TraceConfig();

        [Fact]
        public void DefaultActiveTraceParams()
        {
            Assert.Equal(TraceParams.DEFAULT, traceConfig.ActiveTraceParams);
        }

        [Fact]
        public void UpdateTraceParams()
        {
            TraceParams traceParams =
                TraceParams.DEFAULT
                    .ToBuilder()
                    .SetSampler(Samplers.AlwaysSample)
                    .SetMaxNumberOfAttributes(8)
                    .SetMaxNumberOfAnnotations(9)
                    //.SetMaxNumberOfNetworkEvents(10)
                    .SetMaxNumberOfLinks(11)
                    .Build();
            traceConfig.UpdateActiveTraceParams(traceParams);
            Assert.Equal(traceParams, traceConfig.ActiveTraceParams);
            traceConfig.UpdateActiveTraceParams(TraceParams.DEFAULT);
            Assert.Equal(TraceParams.DEFAULT, traceConfig.ActiveTraceParams);
        }
    }
}
