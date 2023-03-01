// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.MetricCollectors;
using Steeltoe.Management.MetricCollectors.Exporters.Steeltoe;

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
        return JsonSerializer.Serialize(value, GetSerializerOptions());
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

    internal AggregationManager GetTestMetrics(SteeltoeExporter steeltoeExporter)
    {
        var aggregator = new AggregationManager(100, 100, steeltoeExporter.AddMetrics, instrument =>
        {
        }, instrument =>
        {
        }, instrument =>
        {
        }, () =>
        {
        }, () =>
        {
        }, () =>
        {
        }, ex =>
        {
            throw ex;
        });

        aggregator.Include(SteeltoeMetrics.InstrumentationName);

        steeltoeExporter.Collect = aggregator.Collect;

        return aggregator;
    }
    protected static IOptionsMonitor<TOptions> GetOptionsMonitorFromSettings<TOptions, TConfigureOptions>() => GetOptionsMonitorFromSettings<TOptions, TConfigureOptions>(new Dictionary<string, string>());
    protected static IOptionsMonitor<TOptions> GetOptionsMonitorFromSettings<TOptions, TConfigureOptions>(Dictionary<string, string> appsettings)
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.ConfigureOptions(typeof(TConfigureOptions));

        var provider = services.BuildServiceProvider();
        var opts = provider.GetService<IOptionsMonitor<TOptions>>();
        return opts;
    }

    protected static TOptions GetOptionsFromSettings<TOptions, TConfigureOptions>() => GetOptionsMonitorFromSettings<TOptions, TConfigureOptions>().CurrentValue;

    protected static TOptions GetOptionsFromSettings<TOptions, TConfigureOptions>(Dictionary<string, string> appSettings) => GetOptionsMonitorFromSettings<TOptions, TConfigureOptions>(appSettings).CurrentValue;
}
