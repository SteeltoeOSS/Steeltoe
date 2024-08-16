// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Configuration.ConfigServer;

internal sealed class ConfigServerHealthContributor : IHealthContributor
{
    private readonly ILogger<ConfigServerHealthContributor> _logger;

    internal ConfigServerConfigurationProvider? Provider { get; }
    internal ConfigEnvironment? Cached { get; set; }
    internal long LastAccess { get; set; }
    public string Id => "config-server";

    public ConfigServerHealthContributor(IConfiguration configuration, ILogger<ConfigServerHealthContributor> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        Provider = configuration.FindConfigurationProvider<ConfigServerConfigurationProvider>();

        if (Provider == null)
        {
            _logger.LogWarning("Unable to find ConfigServerConfigurationProvider, health check disabled");
        }
    }

    public async Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken)
    {
        var health = new HealthCheckResult();

        if (Provider == null)
        {
            _logger.LogDebug("No Config Server provider found");
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
            _logger.LogDebug("No property sources found");
            health.Status = HealthStatus.Unknown;
            health.Details.Add("error", "No property sources found");
            return health;
        }

        UpdateHealth(health, sources);
        return health;
    }

    internal void UpdateHealth(HealthCheckResult health, IList<PropertySource> sources)
    {
        _logger.LogDebug("Config Server health check returning UP");

        health.Status = HealthStatus.Up;
        var names = new List<string?>();

        foreach (PropertySource source in sources)
        {
            _logger.LogDebug("Returning property source: {PropertySource}", source.Name);
            names.Add(source.Name);
        }

        health.Details.Add("propertySources", names);
    }

    internal async Task<IList<PropertySource>?> GetPropertySourcesAsync(ConfigServerConfigurationProvider provider, CancellationToken cancellationToken)
    {
        long currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        if (IsCacheStale(currentTime))
        {
            LastAccess = currentTime;
            _logger.LogDebug("Cache stale, fetching config server health");
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
}
