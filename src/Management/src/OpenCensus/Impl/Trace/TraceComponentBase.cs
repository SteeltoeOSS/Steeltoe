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
using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Export;
using Steeltoe.Management.Census.Trace.Propagation;
using System;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class TraceComponentBase : ITraceComponent
    {
        internal static ITraceComponent NewNoopTraceComponent
        {
            get
            {
                return new NoopTraceComponent();
            }
        }

        public abstract ITracer Tracer { get; }

        public abstract IPropagationComponent PropagationComponent { get; }

        public abstract IClock Clock { get; }

        public abstract IExportComponent ExportComponent { get; }

        public abstract ITraceConfig TraceConfig { get; }
    }
}
