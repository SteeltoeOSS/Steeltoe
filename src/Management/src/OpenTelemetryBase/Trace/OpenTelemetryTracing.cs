// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
                    sampler = new AlwaysOnSampler();
                }
                else if (options.NeverSample)
                {
                    sampler = new AlwaysOffSampler();
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
