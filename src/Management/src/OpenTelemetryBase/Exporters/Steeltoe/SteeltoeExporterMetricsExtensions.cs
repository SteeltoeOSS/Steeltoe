// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Metrics;

namespace Steeltoe.Management.OpenTelemetry.Exporters;

public static class SteeltoeExporterMetricsExtensions
{
    public static MeterProviderBuilder AddSteeltoeExporter(this MeterProviderBuilder builder, SteeltoeExporter exporter)
    {
        SteeltoeExporter steeltoeExporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        var reader = new BaseExportingMetricReader(steeltoeExporter);
        return builder?.AddReader(reader) ?? throw new ArgumentNullException(nameof(builder));
    }
}
