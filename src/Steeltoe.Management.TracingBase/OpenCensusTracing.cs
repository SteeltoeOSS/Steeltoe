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

using Steeltoe.Management.Census.Trace;
using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Export;
using Steeltoe.Management.Census.Trace.Propagation;
using Steeltoe.Management.Census.Trace.Sampler;

namespace Steeltoe.Management.Tracing
{
    public class OpenCensusTracing : ITracing
    {
        private ITracingOptions options;

        public OpenCensusTracing(ITracingOptions options, ISampler sampler = null)
        {
            this.options = options;
            var builder = TraceParams.DEFAULT.ToBuilder();

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

        public IPropagationComponent PropagationComponent
        {
            get
            {
                return traceComponent.PropagationComponent;
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
