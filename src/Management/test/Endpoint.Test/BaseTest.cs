// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.Json;

namespace Steeltoe.Management.Endpoint.Test;

public abstract class BaseTest : IDisposable
{
    protected static JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate
    }.AddJsonIgnoreEmptyCollection();

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
        return JsonSerializer.Serialize(value, SerializerOptions);
    }

    protected static IOptionsMonitor<TOptions> GetOptionsMonitorFromSettings<TOptions, TConfigureOptions>()
    {
        return GetOptionsMonitorFromSettings<TOptions, TConfigureOptions>([]);
    }

    protected static IOptionsMonitor<TOptions> GetOptionsMonitorFromSettings<TOptions, TConfigureOptions>(Dictionary<string, string?> settings)
    {
        return GetOptionsMonitorFromSettings<TOptions>(typeof(TConfigureOptions), settings);
    }

    protected static IOptionsMonitor<TOptions> GetOptionsMonitorFromSettings<TOptions>(Dictionary<string, string?> settings)
    {
        Type optionsType = typeof(TOptions);

        string configureTypeName = $"{optionsType.Namespace}.Configure{optionsType.Name}";
        Type? configureType = optionsType.Assembly.GetType(configureTypeName);

        if (configureType == null)
        {
            throw new InvalidOperationException($"Could not find type {configureTypeName} in assembly {optionsType.Assembly.FullName}.");
        }

        return GetOptionsMonitorFromSettings<TOptions>(configureType, settings);
    }

    private static IOptionsMonitor<TOptions> GetOptionsMonitorFromSettings<TOptions>(Type configureOptionsType, Dictionary<string, string?> settings)
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(settings);
        IConfiguration configuration = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddApplicationInstanceInfo();
        services.ConfigureOptions(configureOptionsType);
        services.AddLogging();

        using ServiceProvider provider = services.BuildServiceProvider(true);
        return provider.GetRequiredService<IOptionsMonitor<TOptions>>();
    }

    protected static IOptionsMonitor<TOptions> GetOptionsMonitorFromSettings<TOptions>()
    {
        return GetOptionsMonitorFromSettings<TOptions>([]);
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
