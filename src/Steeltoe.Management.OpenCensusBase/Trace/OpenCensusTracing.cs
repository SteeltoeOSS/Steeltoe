// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using OpenCensus.Trace;
using OpenCensus.Trace.Config;
using OpenCensus.Trace.Export;
using OpenCensus.Trace.Propagation;
using OpenCensus.Trace.Sampler;
using Steeltoe.Management.Census.Trace.Propagation;

namespace Steeltoe.Management.Census.Trace
{
    public class OpenCensusTracing  : ITracing
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

        private ITraceComponent traceComponent = new TraceComponent();

        public ITracer Tracer
        {
            get
            {
                return traceComponent.Tracer;
            }
        }

        private IPropagationComponent propagation = new B3PropagationComponent();

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
