// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Export;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Steeltoe.Management.TracingCore
{
    public static class SpanSdkHelper
    {
        public static SpanData ToSpanData(this TelemetrySpan span)
        {
            var spanData = (SpanData)GetGetSpanSdkType().GetField("spanData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(span);
            return spanData;
        }

        public static bool HasEnded(this TelemetrySpan span)
        {
            var hasEnded = (bool)GetGetSpanSdkType().GetField("hasEnded", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(span);
            return hasEnded;
        }

        public static bool IsSampled(this ActivityTraceFlags activityTraceFlags)
        {
            return (activityTraceFlags & ActivityTraceFlags.Recorded) != 0;
        }

        private static Lazy<Type> _spanSdk = new Lazy<Type>(() =>
        {
            Console.WriteLine("test");
            return Assembly.Load("OpenTelemetry").GetType("OpenTelemetry.Trace.SpanSdk");
        });

        private static Type GetGetSpanSdkType()
        {
            return _spanSdk.Value;
        }
    }
}
