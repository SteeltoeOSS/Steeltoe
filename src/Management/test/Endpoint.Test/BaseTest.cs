// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Reflection;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Metrics.SystemDiagnosticsMetrics;

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
    }

    protected string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, GetSerializerOptions());
    }

    protected JsonSerializerOptions GetSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.Converters.Add(new HealthConverter());
        return options;
    }

    internal AggregationManager GetTestMetrics(MetricsExporter exporter)
    {
        var aggregationManager = new AggregationManager(100, 100, exporter.AddMetrics, (_, _) =>
        {
        }, (_, _) =>
        {
        }, _ =>
        {
        }, _ =>
        {
        }, _ =>
        {
        }, () =>
        {
        }, exception => throw exception, () =>
        {
        }, () =>
        {
        }, exception => throw exception);

        aggregationManager.Include(SteeltoeMetrics.InstrumentationName);

        exporter.SetCollect(aggregationManager.Collect);

        return aggregationManager;
    }

    protected static IOptionsMonitor<TOptions> GetOptionsMonitorFromSettings<TOptions, TConfigureOptions>()
    {
        return GetOptionsMonitorFromSettings<TOptions, TConfigureOptions>(new Dictionary<string, string?>());
    }

    protected static IOptionsMonitor<TOptions> GetOptionsMonitorFromSettings<TOptions, TConfigureOptions>(Dictionary<string, string?> settings)
    {
        return GetOptionsMonitorFromSettings<TOptions>(typeof(TConfigureOptions), settings);
    }

    protected static IOptionsMonitor<TOptions> GetOptionsMonitorFromSettings<TOptions>(Dictionary<string, string?> settings)
    {
        Type optionsType = typeof(TOptions);

        Type type = ReflectionHelpers.FindType(new[]
        {
            optionsType.Assembly.FullName
        }, new[]
        {
            $"{optionsType.Namespace}.Configure{optionsType.Name}"
        });

        if (type == null)
        {
            throw new InvalidOperationException($"Could not find Type Configure{typeof(TOptions).Name} in assembly {optionsType.Assembly.FullName}");
        }

        return GetOptionsMonitorFromSettings<TOptions>(type, settings);
    }

    private static IOptionsMonitor<TOptions> GetOptionsMonitorFromSettings<TOptions>(Type configureOptionsType, Dictionary<string, string?> settings)
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(settings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.AddSingleton<IApplicationInstanceInfo>(new ApplicationInstanceInfo(configurationRoot, string.Empty));
        services.ConfigureOptions(configureOptionsType);

        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptionsMonitor<TOptions>>();
    }

    protected static IOptionsMonitor<TOptions> GetOptionsMonitorFromSettings<TOptions>()
    {
        return GetOptionsMonitorFromSettings<TOptions>(new Dictionary<string, string?>());
    }

    protected static TOptions GetOptionsFromSettings<TOptions, TConfigureOptions>()
    {
        return GetOptionsMonitorFromSettings<TOptions, TConfigureOptions>().CurrentValue;
    }

    protected static TOptions GetOptionsFromSettings<TOptions, TConfigureOptions>(Dictionary<string, string?> settings)
    {
        return GetOptionsMonitorFromSettings<TOptions, TConfigureOptions>(settings).CurrentValue;
    }

    protected static TOptions GetOptionsFromSettings<TOptions>()
    {
        return GetOptionsMonitorFromSettings<TOptions>().CurrentValue;
    }

    protected static TOptions GetOptionsFromSettings<TOptions>(Dictionary<string, string?> settings)
    {
        return GetOptionsMonitorFromSettings<TOptions>(settings).CurrentValue;
    }
}
