// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry;
using OpenTelemetry.Metrics;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Exporters.Prometheus;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace Steeltoe.Management.OpenTelemetry
{
    public static class OpenTelemetryMetrics
    {
        public static readonly AssemblyName AssemblyName = typeof(OpenTelemetryMetrics).Assembly.GetName();
        public static readonly string InstrumentationName = AssemblyName.Name;
        public static readonly string InstrumentationVersion = AssemblyName.Version.ToString();

        public static Meter Meter => new Meter(InstrumentationName, InstrumentationVersion);

        public static MeterProvider Initialize(IViewRegistry viewRegistry, SteeltoeExporter steeltoeExporter = null, SteeltoePrometheusExporter prometheusExporter = null, string name = null, string version = null)
        {
            var builder = Sdk.CreateMeterProviderBuilder()
                .AddMeter(name ?? InstrumentationName, version ?? InstrumentationVersion)
                .AddRegisteredViews(viewRegistry);

            if (steeltoeExporter != null)
            {
                builder = builder.AddSteeltoeExporter(steeltoeExporter);
            }

            if (prometheusExporter != null)
            {
                builder = builder.AddReader(new BaseExportingMetricReader(prometheusExporter));
            }

            return builder.Build();
        }
    }
}
