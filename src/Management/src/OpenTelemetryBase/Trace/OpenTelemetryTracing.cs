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

namespace Steeltoe.Management.OpenTelemetry.Trace
{
    public class OpenTelemetryTracing : ITracing
    {
        private readonly ITracingOptions options;

        public OpenTelemetryTracing(ITracingOptions options, Sampler sampler = null)
        {
            this.options = options;

            var factory = TracerFactory.Create(builder =>
            {
                if (sampler != null)
                {
                    builder.SetSampler(sampler);
                }
                else if (options.AlwaysSample)
                {
                    builder.SetSampler(new AlwaysSampleSampler());
                }
                else if (options.NeverSample)
                {
                    builder.SetSampler(new NeverSampleSampler());
                }

                if (options.MaxNumberOfAttributes > 0 &&
                    options.MaxNumberOfLinks > 0 &&
                    options.MaxNumberOfMessageEvents > 0)
                {
                    builder.SetTracerOptions(new TracerConfiguration(
                     options.MaxNumberOfAttributes,
                     options.MaxNumberOfMessageEvents,
                     options.MaxNumberOfLinks));
                }
            });

            _tracer = factory.GetTracer("SteeltoeTracer");
        }

        //   private readonly ITraceComponent traceComponent = new TraceComponent();

        private Tracer _tracer;

        public Tracer Tracer
        {
            get
            {
                return _tracer;// traceComponent.Tracer;
            }
        }

        public ITextFormat TextFormat { get; } = new B3Format();

        //public IExportComponent ExportComponent
        //{
        //    get
        //    {
        //        return traceComponent.ExportComponent;
        //    }
        //}

        //public TraceConfig TraceConfig
        //{
        //    get
        //    {
        //        return traceComponent.TraceConfig;
        //    }
        //}
    }
}
