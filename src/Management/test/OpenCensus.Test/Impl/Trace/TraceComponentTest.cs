﻿// <copyright file="TraceComponentTest.cs" company="OpenCensus Authors">
// Copyright 2018, OpenCensus Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenCensus.Trace.Test
{
    using OpenCensus.Common;
    using OpenCensus.Internal;
    using OpenCensus.Trace.Export;
    using OpenCensus.Trace.Internal;
    using OpenCensus.Trace.Propagation;
    using Xunit;

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
            Assert.IsType<DefaultPropagationComponent>(traceComponent.PropagationComponent);
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