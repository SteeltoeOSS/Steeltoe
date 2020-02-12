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

using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Configuration;
using OpenTelemetry.Trace.Samplers;
using System;

namespace Steeltoe.Management.OpenTelemetry.Trace
{
    public class OpenTelemetryTracing : ITracing
    {
        private const int DefaultSpanMaxNumAttributes = 32;

        private const int DefaultSpanMaxNumEvents = 128;

        private const int DefaultSpanMaxNumLinks = 32;

        public OpenTelemetryTracing(ITracingOptions options, Action<TracerBuilder> configureTracer = null)
        {
            var factory = TracerFactory.Create(builder =>
            {
                if (options.AlwaysSample)
                {
                    builder.SetSampler(new AlwaysSampleSampler());
                }
                else if (options.NeverSample)
                {
                    builder.SetSampler(new NeverSampleSampler());
                }

                var tracerConfig = new TracerConfiguration();
                if (options.MaxNumberOfAttributes > 0 ||
                    options.MaxNumberOfLinks > 0 ||
                    options.MaxNumberOfMessageEvents > 0)
                {
                    var maxAttributes = options.MaxNumberOfAttributes > 0 ? options.MaxNumberOfAttributes : DefaultSpanMaxNumAttributes;
                    var maxEvents = options.MaxNumberOfMessageEvents > 0 ? options.MaxNumberOfMessageEvents : DefaultSpanMaxNumEvents;
                    var maxLinks = options.MaxNumberOfLinks > 0 ? options.MaxNumberOfLinks : DefaultSpanMaxNumLinks;
                    tracerConfig = new TracerConfiguration(maxAttributes, maxEvents, maxLinks);
                }

                builder.SetTracerOptions(tracerConfig);

                configureTracer?.Invoke(builder);
            });

            Tracer = factory.GetTracer(options.Name);
        }

        public Tracer Tracer { get; }

        public ITextFormat TextFormat { get; } = new B3Format();
    }
}
