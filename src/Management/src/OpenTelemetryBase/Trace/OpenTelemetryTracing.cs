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
        private const int DefaultMaxAttributes = 32;

        private const int DefaultMaxEvents = 128;

        private const int DefaultMaxLinks = 32;

        public OpenTelemetryTracing(ITracingOptions options, Action<TracerBuilder> configureTracer = null)
        {
            var maxAttributes = options.MaxNumberOfAttributes > 0 ? options.MaxNumberOfAttributes : DefaultMaxAttributes;
            var maxEvents = options.MaxNumberOfMessageEvents > 0 ? options.MaxNumberOfMessageEvents : DefaultMaxEvents;
            var maxLinks = options.MaxNumberOfLinks > 0 ? options.MaxNumberOfLinks : DefaultMaxLinks;

            TracerConfiguration = new TracerConfiguration(maxAttributes, maxEvents, maxLinks);

            var factory = TracerFactory.Create(ConfigureOptions(options, configureTracer));

            Tracer = factory.GetTracer(options.Name);
        }

        private Action<TracerBuilder> ConfigureOptions(ITracingOptions options, Action<TracerBuilder> configureTracer)
        {
            return builder =>
            {
                Sampler sampler = null;
                if (options.AlwaysSample)
                {
                    sampler = new AlwaysSampleSampler();
                }
                else if (options.NeverSample)
                {
                    sampler = new NeverSampleSampler();
                }

                builder.SetTracerOptions(TracerConfiguration);

                if (sampler != null)
                {
                    ConfiguredSampler = sampler;
                    builder.SetSampler(sampler);
                }

                configureTracer?.Invoke(builder);
            };
        }

        public Tracer Tracer { get; }

        public ITextFormat TextFormat { get; } = new B3Format();

        public TracerConfiguration TracerConfiguration { get; }

        /// <summary>
        /// Gets sampler configured from Options. If a sampler is setup in the builder this property will not reflect it.
        /// </summary>
        public Sampler ConfiguredSampler { get; private set; }
    }
}
