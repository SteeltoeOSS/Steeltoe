// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenCensus.Trace;
using OpenCensus.Trace.Config;
using OpenCensus.Trace.Export;
using OpenCensus.Trace.Propagation;
using OpenCensus.Trace.Sampler;
using Steeltoe.Management.Census.Trace.Propagation;

namespace Steeltoe.Management.Census.Trace
{
    public class OpenCensusTracing : ITracing
    {
        private readonly ITracingOptions options;

        public OpenCensusTracing(ITracingOptions options, ISampler sampler = null)
        {
            this.options = options;
            var builder = TraceParams.Default.ToBuilder();

            if (sampler != null)
            {
                builder.SetSampler(sampler);
            }
            else if (options.AlwaysSample)
            {
                builder.SetSampler(Samplers.AlwaysSample);
            }
            else if (options.NeverSample)
            {
                builder.SetSampler(Samplers.NeverSample);
            }

            if (options.MaxNumberOfAnnotations > 0)
            {
                builder.SetMaxNumberOfAnnotations(options.MaxNumberOfAnnotations);
            }

            if (options.MaxNumberOfAttributes > 0)
            {
                builder.SetMaxNumberOfAttributes(options.MaxNumberOfAttributes);
            }

            if (options.MaxNumberOfLinks > 0)
            {
                builder.SetMaxNumberOfLinks(options.MaxNumberOfLinks);
            }

            if (options.MaxNumberOfMessageEvents > 0)
            {
                builder.SetMaxNumberOfMessageEvents(options.MaxNumberOfMessageEvents);
            }

            TraceConfig.UpdateActiveTraceParams(builder.Build());
        }

        private readonly ITraceComponent traceComponent = new TraceComponent();

        public ITracer Tracer
        {
            get
            {
                return traceComponent.Tracer;
            }
        }

        private readonly IPropagationComponent propagation = new B3PropagationComponent();

        public IPropagationComponent PropagationComponent
        {
            get
            {
                return propagation;
            }
        }

        public IExportComponent ExportComponent
        {
            get
            {
                return traceComponent.ExportComponent;
            }
        }

        public ITraceConfig TraceConfig
        {
            get
            {
                return traceComponent.TraceConfig;
            }
        }
    }
}
