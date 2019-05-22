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

using Steeltoe.Management.Census.Internal;
using System;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Export.Test
{
    [Obsolete]
    public class ExportComponentTest
    {
        private readonly IExportComponent exportComponentWithInProcess = ExportComponent.CreateWithInProcessStores(new SimpleEventQueue());
        private readonly IExportComponent exportComponentWithoutInProcess = ExportComponent.CreateWithoutInProcessStores(new SimpleEventQueue());

        [Fact]
        public void ImplementationOfSpanExporter()
        {
            Assert.IsType<SpanExporter>(exportComponentWithInProcess.SpanExporter);
        }

        [Fact]
        public void ImplementationOfActiveSpans()
        {
            Assert.IsType<InProcessRunningSpanStore>(exportComponentWithInProcess.RunningSpanStore);
            Assert.IsType<NoopRunningSpanStore>(exportComponentWithoutInProcess.RunningSpanStore);
        }

        [Fact]
        public void ImplementationOfSampledSpanStore()
        {
            Assert.IsType<InProcessSampledSpanStore>(exportComponentWithInProcess.SampledSpanStore);
            Assert.IsType<NoopSampledSpanStore>(exportComponentWithoutInProcess.SampledSpanStore);
        }
    }
}
