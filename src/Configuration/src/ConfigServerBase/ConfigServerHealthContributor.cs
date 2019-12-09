// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Extensions.Configuration.Placeholder;
using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration.ConfigServer
{
    public class ConfigServerHealthContributor : IHealthContributor
    {
        public static IHealthContributor GetHealthContributor(IConfiguration configuration, ILogger<ConfigServerHealthContributor> logger = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            return new ConfigServerHealthContributor(configuration, logger);
        }

        public string Id => "config-server";

        internal ConfigServerConfigurationProvider Provider { get; set; }

        internal ConfigEnvironment Cached { get; set; }

        internal long LastAccess { get; set; }

        internal IConfiguration Configuration { get; set; }

        internal ILogger<ConfigServerHealthContributor> Logger { get; set; }

        public ConfigServerHealthContributor(IConfiguration configuration, ILogger<ConfigServerHealthContributor> logger = null)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Logger = logger;
            Provider = FindProvider(configuration);
        }

        public HealthCheckResult Health()
        {
            var health = new HealthCheckResult();

            if (Provider == null)
            {
                Logger?.LogDebug("No config server provider found");
                health.Status = HealthStatus.UNKNOWN;
                health.Details.Add("error", "No config server provider found");
                return health;
            }

            if (!IsEnabled())
            {
                Logger?.LogDebug("Config server health check disabled");
                health.Status = HealthStatus.UNKNOWN;
                health.Details.Add("info", "Health check disabled");
                return health;
            }

            IList<PropertySource> sources = GetPropertySources();
            if (sources == null || sources.Count == 0)
            {
                Logger?.LogDebug("No property sources found");
                health.Status = HealthStatus.UNKNOWN;
                health.Details.Add("error", "No property sources found");
                return health;
            }

            UpdateHealth(health, sources);
            return health;
        }

        internal void UpdateHealth(HealthCheckResult health, IList<PropertySource> sources)
        {
            Logger?.LogDebug("Config server health check returning UP");

            health.Status = HealthStatus.UP;
            List<string> names = new List<string>();
            foreach (var source in sources)
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

            return (accessTime - LastAccess) >= GetTimeToLive();
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
                foreach (var provider in root.Providers)
                {
                    if (provider is PlaceholderResolverProvider placeholder)
                    {
                        result = FindProvider(placeholder.Configuration);
                        break;
                    }
                    else
                    {
                        if (provider is ConfigServerConfigurationProvider)
                        {
                            result = provider as ConfigServerConfigurationProvider;
                            break;
                        }
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
}
