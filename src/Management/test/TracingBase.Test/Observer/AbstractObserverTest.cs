// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenCensus.Trace;

namespace Steeltoe.Management.Tracing.Observer.Test
{
    public abstract class AbstractObserverTest
    {
        protected Span GetCurrentSpan(ITracer tracer)
        {
            var span = tracer.CurrentSpan;
            if (span.Context == OpenCensus.Trace.SpanContext.Invalid)
            {
                return null;
            }

            return span as Span;
        }
    }
}
