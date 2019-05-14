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
    public sealed class TraceComponent : TraceComponentBase
    {
        public TraceComponent()
            : this(DateTimeOffsetClock.INSTANCE, new RandomGenerator(), new SimpleEventQueue())
        {
        }

        public TraceComponent(IClock clock, IRandomGenerator randomHandler, IEventQueue eventQueue)
        {
            Clock = clock;
            TraceConfig = new Config.TraceConfig();

            // TODO(bdrutu): Add a config/argument for supportInProcessStores.
            if (eventQueue is SimpleEventQueue)
            {
                ExportComponent = Export.ExportComponent.CreateWithoutInProcessStores(eventQueue);
            }
            else
            {
                ExportComponent = Export.ExportComponent.CreateWithInProcessStores(eventQueue);
            }

            PropagationComponent = new PropagationComponent();
            IStartEndHandler startEndHandler =
                new StartEndHandler(
                    ExportComponent.SpanExporter,
                    ExportComponent.RunningSpanStore,
                    ExportComponent.SampledSpanStore,
                    eventQueue);
            Tracer = new Tracer(randomHandler, startEndHandler, clock, TraceConfig);
        }

        public override ITracer Tracer { get; }

        public override IPropagationComponent PropagationComponent { get; }

        public override IClock Clock { get; }

        public override IExportComponent ExportComponent { get; }

        public override ITraceConfig TraceConfig { get; }
    }
}
