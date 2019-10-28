﻿// <copyright file="TraceComponent.cs" company="OpenCensus Authors">
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

namespace OpenCensus.Trace
{
    using OpenCensus.Common;
    using OpenCensus.Internal;
    using OpenCensus.Trace.Config;
    using OpenCensus.Trace.Export;
    using OpenCensus.Trace.Internal;
    using OpenCensus.Trace.Propagation;

    /// <summary>
    /// Trace component holds all the extensibility points required for distributed tracing.
    /// </summary>
    public sealed class TraceComponent : ITraceComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TraceComponent"/> class.
        /// </summary>
        public TraceComponent()
            : this(DateTimeOffsetClock.Instance, new RandomGenerator(), new SimpleEventQueue())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceComponent"/> class.
        /// </summary>
        /// <param name="clock">Clock to use to get the current time.</param>
        /// <param name="randomHandler">Random numbers generator.</param>
        /// <param name="eventQueue">Event queue to use before the exporter.</param>
        public TraceComponent(IClock clock, IRandomGenerator randomHandler, IEventQueue eventQueue)
        {
            this.Clock = clock;
            this.TraceConfig = new Config.TraceConfig();

            // TODO(bdrutu): Add a config/argument for supportInProcessStores.
            if (eventQueue is SimpleEventQueue)
            {
                this.ExportComponent = Export.ExportComponent.CreateWithoutInProcessStores(eventQueue);
            }
            else
            {
                this.ExportComponent = Export.ExportComponent.CreateWithInProcessStores(eventQueue);
            }

            this.PropagationComponent = new DefaultPropagationComponent();
            IStartEndHandler startEndHandler =
                new StartEndHandler(
                    this.ExportComponent.SpanExporter,
                    this.ExportComponent.RunningSpanStore,
                    this.ExportComponent.SampledSpanStore,
                    eventQueue);
            this.Tracer = new Tracer(randomHandler, startEndHandler, clock, this.TraceConfig);
        }

        /// <inheritdoc/>
        public ITracer Tracer { get; }

        /// <inheritdoc/>
        public IPropagationComponent PropagationComponent { get; }

        /// <inheritdoc/>
        public IClock Clock { get; }

        /// <inheritdoc/>
        public IExportComponent ExportComponent { get; }

        /// <inheritdoc/>
        public ITraceConfig TraceConfig { get; }
    }
}
