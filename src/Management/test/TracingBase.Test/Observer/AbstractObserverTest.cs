// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Trace;

namespace Steeltoe.Management.Tracing.Observer.Test
{
    public abstract class AbstractObserverTest
    {
        protected TelemetrySpan GetCurrentSpan(Tracer tracer)
        {
            var span = tracer.CurrentSpan;
            return span.Context.IsValid ? span : null;
        }
    }
}
