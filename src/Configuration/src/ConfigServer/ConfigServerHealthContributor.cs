// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Extensions.Configuration.Placeholder;

namespace Steeltoe.Extensions.Configuration.ConfigServer;

public class ConfigServerHealthContributor : IHealthContributor
{
    internal ConfigServerConfigurationProvider Provider { get; set; }

    internal ConfigEnvironment Cached { get; set; }

    internal long LastAccess { get; set; }

    internal IConfiguration Configuration { get; set; }

    internal ILogger<ConfigServerHealthContributor> Logger { get; set; }

    public string Id => "config-server";

    public ConfigServerHealthContributor(IConfiguration configuration, ILogger<ConfigServerHealthContributor> logger = null)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Logger = logger;
        Provider = FindProvider(configuration);
    }

    public static IHealthContributor GetHealthContributor(IConfiguration configuration, ILogger<ConfigServerHealthContributor> logger = null)
    {
        ArgumentGuard.NotNull(configuration);

        return new ConfigServerHealthContributor(configuration, logger);
    }

    public HealthCheckResult Health()
    {
        var health = new HealthCheckResult();

        if (Provider == null)
        {
            Logger?.LogDebug("No config server provider found");
            health.Status = HealthStatus.Unknown;
            health.Details.Add("error", "No config server provider found");
            return health;
        }

        if (!IsEnabled())
        {
            Logger?.LogDebug("Config server health check disabled");
            health.Status = HealthStatus.Unknown;
            health.Details.Add("info", "Health check disabled");
            return health;
        }

        IList<PropertySource> sources = GetPropertySources();

        if (sources == null || sources.Count == 0)
        {
            Logger?.LogDebug("No property sources found");
            health.Status = HealthStatus.Unknown;
            health.Details.Add("error", "No property sources found");
            return health;
        }

        UpdateHealth(health, sources);
        return health;
    }

    internal void UpdateHealth(HealthCheckResult health, IList<PropertySource> sources)
    {
        Logger?.LogDebug("Config server health check returning UP");

        health.Status = HealthStatus.Up;
        var names = new List<string>();

        foreach (PropertySource source in sources)
        {
            Logger?.LogDebug("Returning property source: {propertySource}", source.Name);
            names.Add(source.Name);
        }

        health.Details.Add("propertySources", names);
    }

    internal IList<PropertySource> GetPropertySources()
    {
        long currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        if (IsCacheStale(currentTime))
        {
            LastAccess = currentTime;
            Logger?.LogDebug("Cache stale, fetching config server health");
            Cached = Provider.LoadInternal(false);
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
        return Provider.Settings.HealthEnabled;
    }

    internal long GetTimeToLive()
    {
        return Provider.Settings.HealthTimeToLive;
    }

    internal ConfigServerConfigurationProvider FindProvider(IConfiguration configuration)
    {
        ConfigServerConfigurationProvider result = null;

        if (configuration is IConfigurationRoot root)
        {
            foreach (IConfigurationProvider provider in root.Providers)
            {
                if (provider is PlaceholderResolverProvider placeholder)
                {
                    result = FindProvider(placeholder.Configuration);
                    break;
                }

                if (provider is ConfigServerConfigurationProvider configServer)
                {
                    result = configServer;
                    break;
                }
            }
        }

        if (result == null)
        {
            Logger?.LogWarning("Unable to find ConfigServerConfigurationProvider, health check disabled");
        }

        return result;
    }
}
