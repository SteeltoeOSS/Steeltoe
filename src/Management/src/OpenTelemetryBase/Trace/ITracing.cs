// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Configuration;

namespace Steeltoe.Management.OpenTelemetry.Trace
{
    public interface ITracing
    {
        Tracer Tracer { get; }

        ITextFormat TextFormat { get; }

        TracerConfiguration TracerConfiguration { get; }

        Sampler ConfiguredSampler { get; }
    }
}
