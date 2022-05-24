// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry;
using OpenTelemetry.Metrics;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Exporters.Prometheus;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;

namespace Steeltoe.Management.OpenTelemetry
{
    public static class OpenTelemetryMetrics
    {
        public static readonly AssemblyName AssemblyName = typeof(OpenTelemetryMetrics).Assembly.GetName();

        public static readonly string InstrumentationVersion = AssemblyName.Version.ToString();

        public static Meter Meter => new (InstrumentationName, InstrumentationVersion);

        public static string InstrumentationName { get; set; } = AssemblyName.Name;
    }
}
