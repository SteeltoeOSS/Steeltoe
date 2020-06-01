// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenCensus.Trace;
using OpenCensus.Trace.Config;
using OpenCensus.Trace.Export;
using OpenCensus.Trace.Propagation;

namespace Steeltoe.Management.Census.Trace
{
    public interface ITracing
    {
        ITracer Tracer { get; }

        IPropagationComponent PropagationComponent { get; }

        IExportComponent ExportComponent { get; }

        ITraceConfig TraceConfig { get; }
    }
}
