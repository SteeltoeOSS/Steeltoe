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

using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Export;
using Steeltoe.Management.Census.Trace.Propagation;
using System;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    [Obsolete]
    public class TraceComponentBaseTest
    {
        [Fact]
        public void DefaultTracer()
        {
            Assert.Same(Tracer.NoopTracer, TraceComponentBase.NewNoopTraceComponent.Tracer);
        }

        [Fact]
        public void DefaultBinaryPropagationHandler()
        {
            Assert.Same(PropagationComponentBase.NoopPropagationComponent, TraceComponentBase.NewNoopTraceComponent.PropagationComponent);
        }

        [Fact]
        public void DefaultClock()
        {
            Assert.True(TraceComponentBase.NewNoopTraceComponent.Clock is ZeroTimeClock);
        }

        [Fact]
        public void DefaultTraceExporter()
        {
            Assert.Equal(ExportComponentBase.NewNoopExportComponent.GetType(), TraceComponentBase.NewNoopTraceComponent.ExportComponent.GetType());
        }

        [Fact]
        public void DefaultTraceConfig()
        {
            Assert.Same(TraceConfigBase.NoopTraceConfig, TraceComponentBase.NewNoopTraceComponent.TraceConfig);
        }
    }
}
