// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.OpenTelemetry.Trace.Propagation
{
    public static class B3Constants
    {
        public const string XB3TraceId = "X-B3-TraceId";
        public const string XB3SpanId = "X-B3-SpanId";
        public const string XB3ParentSpanId = "X-B3-ParentSpanId";
        public const string XB3Sampled = "X-B3-Sampled";
        public const string XB3Flags = "X-B3-Flags";
    }
}
