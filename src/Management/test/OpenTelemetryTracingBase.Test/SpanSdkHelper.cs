using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Export;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Steeltoe.Management.OpenTelemetryTracingBase.Test
{
    public class SpanSdkHelper
    {
        public static SpanInfo GetSpanData(TelemetrySpan span)
        {

            var assembly = Assembly.Load("OpenTelemetry");
            var spanSdk = assembly.GetType("OpenTelemetry.Trace.SpanSdk");
            var spanData = (SpanData)spanSdk.GetField("spanData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(span);
            var hasEnded = (bool)spanSdk.GetField("hasEnded", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(span);
            return new SpanInfo
            {
                SpanData = spanData,
                HasEnded = hasEnded
            };
        }

        public class SpanInfo
        {
            public SpanData SpanData { get; set; }

            public bool HasEnded { get; set; }
        }
    }
}
