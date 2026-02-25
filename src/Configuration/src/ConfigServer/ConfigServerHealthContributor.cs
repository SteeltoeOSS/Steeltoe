// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Configuration.ConfigServer;

internal sealed partial class ConfigServerHealthContributor : IHealthContributor
{
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ConfigServerHealthContributor> _logger;

    internal ConfigServerConfigurationProvider? Provider { get; }
    internal ConfigEnvironment? Cached { get; set; }
    internal long LastAccess { get; set; }
    public string Id => "config-server";

    public ConfigServerHealthContributor(IConfiguration configuration, TimeProvider timeProvider, ILogger<ConfigServerHealthContributor> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _timeProvider = timeProvider;
        _logger = logger;
        Provider = configuration.EnumerateProviders<ConfigServerConfigurationProvider>().FirstOrDefault();

        if (Provider == null)
        {
            LogHealthCheckDisabled();
        }
    }

    public async Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken)
    {
        var health = new HealthCheckResult();

        if (Provider == null)
        {
            LogNoProviderFound();
            health.Status = HealthStatus.Unknown;
            health.Details.Add("error", "No Config Server provider found");
            return health;
        }

        if (!IsEnabled())
        {
            return null;
        }

        IList<PropertySource>? sources = await GetPropertySourcesAsync(Provider, cancellationToken);

        if (sources == null || sources.Count == 0)
        {
            LogNoPropertySourcesFound();
            health.Status = HealthStatus.Unknown;
            health.Details.Add("error", "No property sources found");
            return health;
        }

        UpdateHealth(health, sources);
        return health;
    }

    internal void UpdateHealth(HealthCheckResult health, IList<PropertySource> sources)
    {
        LogHealthCheckReturningUp();

        health.Status = HealthStatus.Up;
        List<string?> names = [];

        foreach (PropertySource source in sources)
        {
            LogReturningPropertySource(source.Name);
            names.Add(source.Name);
        }

        health.Details.Add("propertySources", names);
    }

    internal async Task<IList<PropertySource>?> GetPropertySourcesAsync(ConfigServerConfigurationProvider provider, CancellationToken cancellationToken)
    {
        long currentTime = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();

        if (IsCacheStale(currentTime))
        {
            LastAccess = currentTime;
            LogCacheStale();
            Cached = await provider.LoadInternalAsync(false, cancellationToken);
        }

        return Cached?.PropertySources;
    }

    internal bool IsCacheStale(long accessTime)
    {
        if (Cached == null)
        {
            return true;
        }

        return accessTime - LastAccess >= GetTimeToLive();
    }

    internal bool IsEnabled()
    {
        return Provider is { ClientOptions.Health.Enabled: true };
    }

    internal long GetTimeToLive()
    {
        return Provider != null ? Provider.ClientOptions.Health.TimeToLive : long.MaxValue;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No Config Server provider found, health check disabled.")]
    private partial void LogHealthCheckDisabled();

    [LoggerMessage(Level = LogLevel.Debug, Message = "No Config Server provider found.")]
    private partial void LogNoProviderFound();

    [LoggerMessage(Level = LogLevel.Debug, Message = "No property sources found.")]
    private partial void LogNoPropertySourcesFound();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Config Server health check returning UP.")]
    private partial void LogHealthCheckReturningUp();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Returning property source {PropertySource}.")]
    private partial void LogReturningPropertySource(string? propertySource);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache stale, fetching config server health.")]
    private partial void LogCacheStale();
}
