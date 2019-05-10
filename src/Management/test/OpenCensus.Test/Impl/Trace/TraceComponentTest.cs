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
using Steeltoe.Management.Census.Internal;
using Steeltoe.Management.Census.Trace.Export;
using Steeltoe.Management.Census.Trace.Internal;
using Steeltoe.Management.Census.Trace.Propagation;
using System;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    [Obsolete]
    public class TraceComponentTest
    {
        private readonly TraceComponent traceComponent = new TraceComponent(DateTimeOffsetClock.GetInstance(), new RandomGenerator(), new SimpleEventQueue());

        [Fact]
        public void ImplementationOfTracer()
        {
            Assert.IsType<Tracer>(traceComponent.Tracer);
        }

        [Fact]
        public void IplementationOfBinaryPropagationHandler()
        {
            Assert.IsType<PropagationComponent>(traceComponent.PropagationComponent);
        }

        [Fact]
        public void ImplementationOfClock()
        {
            Assert.IsType<DateTimeOffsetClock>(traceComponent.Clock);
        }

        [Fact]
        public void ImplementationOfTraceExporter()
        {
            Assert.IsType<ExportComponent>(traceComponent.ExportComponent);
        }
    }
}