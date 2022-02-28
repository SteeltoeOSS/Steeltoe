// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Metrics;
using System;

namespace Steeltoe.Management.OpenTelemetry.Exporters
{
    public static class SteeltoeExporterMetricsExtensions
    {
        private static string _statusTagKey = "status";
        private static string _exceptionTagKey = "exception";
        private static string _methodTagKey = "method";
        private static string _uriTagKey = "uri";

        public static MeterProviderBuilder AddSteeltoeExporter(this MeterProviderBuilder builder, SteeltoeExporter exporter)
        {
            var steeltoeExporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
            var reader = new BaseExportingMetricReader(steeltoeExporter);
            return builder?.AddReader(reader) ?? throw new ArgumentNullException(nameof(builder));
        }

        public static MeterProviderBuilder AddAspnetServerInstrumentsAndViews(this MeterProviderBuilder builder) =>
                builder.AddAspNetCoreInstrumentation()
                .AddView("http.server.request.time", new ExplicitBucketHistogramConfiguration()
                {
                    Boundaries = new double[] { 0.0, 1.0, 5.0, 10.0, 100.0 },
                    TagKeys = new string[] { _statusTagKey, _exceptionTagKey, _methodTagKey, _uriTagKey },
                })
                .AddView("http.server.request.count", new ExplicitBucketHistogramConfiguration()
                {
                    Boundaries = new double[] { 0.0, 1.0, 5.0, 10.0, 100.0 },
                    TagKeys = new string[] { _statusTagKey, _exceptionTagKey, _methodTagKey, _uriTagKey },
                });
    }
}
