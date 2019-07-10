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
using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Export;
using Steeltoe.Management.Census.Trace.Internal;
using Steeltoe.Management.Census.Trace.Propagation;
using System;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class Tracing
    {
        private static readonly Tracing _tracing = new Tracing();

        internal Tracing()
            : this(false)
        {
        }

        internal Tracing(bool enabled)
        {
            if (enabled)
            {
                traceComponent = new TraceComponent(DateTimeOffsetClock.INSTANCE, new RandomGenerator(), new SimpleEventQueue());
            }
            else
            {
                traceComponent = TraceComponent.NewNoopTraceComponent;
            }
        }

        private readonly ITraceComponent traceComponent = null;

        public static ITracer Tracer
        {
            get
            {
                return _tracing.traceComponent.Tracer;
            }
        }

        public static IPropagationComponent PropagationComponent
        {
            get
            {
                return _tracing.traceComponent.PropagationComponent;
            }
        }

        public static IExportComponent ExportComponent
        {
            get
            {
                return _tracing.traceComponent.ExportComponent;
            }
        }

        public static ITraceConfig TraceConfig
        {
            get
            {
                return _tracing.traceComponent.TraceConfig;
            }
        }
    }
}
