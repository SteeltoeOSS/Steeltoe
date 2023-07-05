// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Reflection;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.MetricCollectors.Aggregations;
using Steeltoe.Management.MetricCollectors.Exporters.Steeltoe;
using Steeltoe.Management.MetricCollectors.Metrics;

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
        }
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

        steeltoeExporter.SetCollect(aggregator.Collect);

        return aggregator;
    }

    protected static IOptionsMonitor<TOptions> GetOptionsMonitorFromSettings<TOptions, TConfigureOptions>()
    {
        return GetOptionsMonitorFromSettings<TOptions, TConfigureOptions>(new Dictionary<string, string>());
    }

    protected static IOptionsMonitor<TOptions> GetOptionsMonitorFromSettings<TOptions, TConfigureOptions>(Dictionary<string, string> settings)
    {
        return GetOptionsMonitorFromSettings<TOptions>(typeof(TConfigureOptions), settings);
    }

    protected static IOptionsMonitor<TOptions> GetOptionsMonitorFromSettings<TOptions>(Dictionary<string, string> settings)
    {
        Type tOptions = typeof(TOptions);

        Type type = ReflectionHelpers.FindType(new[]
        {
            tOptions.Assembly.FullName
        }, new[]
        {
            $"{tOptions.Namespace}.Configure{tOptions.Name}"
        });

        if (type == null)
        {
            throw new InvalidOperationException($"Could not find Type Configure{typeof(TOptions).Name} in assembly {tOptions.Assembly.FullName}");
        }

        return GetOptionsMonitorFromSettings<TOptions>(type, settings);
    }

    private static IOptionsMonitor<TOptions> GetOptionsMonitorFromSettings<TOptions>(Type tConfigureOptions, Dictionary<string, string> settings)
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(settings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.ConfigureOptions(tConfigureOptions);

        ServiceProvider provider = services.BuildServiceProvider();
        var opts = provider.GetService<IOptionsMonitor<TOptions>>();
        return opts;
    }

    protected static IOptionsMonitor<TOptions> GetOptionsMonitorFromSettings<TOptions>()
    {
        return GetOptionsMonitorFromSettings<TOptions>(new Dictionary<string, string>());
    }

    protected static TOptions GetOptionsFromSettings<TOptions, TConfigureOptions>()
    {
        return GetOptionsMonitorFromSettings<TOptions, TConfigureOptions>().CurrentValue;
    }

    protected static TOptions GetOptionsFromSettings<TOptions, TConfigureOptions>(Dictionary<string, string> settings)
    {
        return GetOptionsMonitorFromSettings<TOptions, TConfigureOptions>(settings).CurrentValue;
    }

    protected static TOptions GetOptionsFromSettings<TOptions>()
    {
        return GetOptionsMonitorFromSettings<TOptions>().CurrentValue;
    }

    protected static TOptions GetOptionsFromSettings<TOptions>(Dictionary<string, string> settings)
    {
        return GetOptionsMonitorFromSettings<TOptions>(settings).CurrentValue;
    }
}
