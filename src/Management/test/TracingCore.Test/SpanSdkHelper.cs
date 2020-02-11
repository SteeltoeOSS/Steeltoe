using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Export;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Steeltoe.Management.TracingCore
{
    public static class SpanSdkHelper
    {
        private static Lazy<Type> _spanSdk = new Lazy<Type>(()=>{
            Console.WriteLine("test");
            return Assembly.Load("OpenTelemetry").GetType("OpenTelemetry.Trace.SpanSdk"); });

        private static Type GetGetSpanSdkType()
        {
            return _spanSdk.Value;
        }

        public static SpanData ToSpanData(this TelemetrySpan span)
        {
            var spanData = (SpanData)GetGetSpanSdkType().GetField("spanData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(span);
            return spanData;
        }

        public static bool hasEnded(this TelemetrySpan span)
        {

            var hasEnded = (bool)GetGetSpanSdkType().GetField("hasEnded", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(span);
            return hasEnded;
        }
    }
}
