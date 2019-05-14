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

using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Sampler;
using System;
using Xunit;

namespace Steeltoe.Management.CensusBase.Trace.Config.Test
{
    [Obsolete]
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
        public void UpdateTraceParams_NonPositiveMaxNumberOfMessageEvents()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TraceParams.DEFAULT.ToBuilder().SetMaxNumberOfMessageEvents(0).Build());
        }

        [Fact]
        public void UpdateTraceParams_NonPositiveMaxNumberOfLinks()
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
