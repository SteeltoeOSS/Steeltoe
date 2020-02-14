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
