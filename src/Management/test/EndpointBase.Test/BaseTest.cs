// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Test;

public abstract class BaseTest : IDisposable
{
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            DiagnosticsManager.Instance.Dispose();
        }
    }

    public ILogger<T> GetLogger<T>()
    {
        var lf = new LoggerFactory();
        return lf.CreateLogger<T>();
    }

    public string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(
            value,
            GetSerializerOptions());
    }

    public JsonSerializerOptions GetSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new HealthConverter());
        options.Converters.Add(new MetricsResponseConverter());
        return options;
    }

    public MeterProvider GetTestMetrics(IViewRegistry viewRegistry, SteeltoeExporter steeltoeExporter, SteeltoePrometheusExporter prometheusExporter, string name = null, string version = null)
    {
        var builder = Sdk.CreateMeterProviderBuilder()
            .AddMeter(name ?? OpenTelemetryMetrics.InstrumentationName, version ?? OpenTelemetryMetrics.InstrumentationVersion)
            .AddRegisteredViews(viewRegistry);
        if (steeltoeExporter != null)
        {
            builder.AddSteeltoeExporter(steeltoeExporter);
        }

        if (prometheusExporter != null)
        {
            builder.AddReader(new BaseExportingMetricReader(prometheusExporter));
        }

        return builder.Build();
    }
}
