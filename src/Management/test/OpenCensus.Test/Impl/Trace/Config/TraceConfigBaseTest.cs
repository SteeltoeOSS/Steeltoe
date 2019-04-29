using Steeltoe.Management.Census.Trace.Sampler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Config.Test
{
    public class TraceConfigBaseTest
    {
        ITraceConfig traceConfig = TraceConfigBase.NoopTraceConfig;

        [Fact]
        public void ActiveTraceParams_NoOpImplementation()
        {
            Assert.Equal(TraceParams.DEFAULT, traceConfig.ActiveTraceParams);
        }

        [Fact]
        public void UpdateActiveTraceParams_NoOpImplementation()
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
            traceConfig.UpdateActiveTraceParams(traceParams);
            Assert.Equal(TraceParams.DEFAULT, traceConfig.ActiveTraceParams);
        }
    }
}
