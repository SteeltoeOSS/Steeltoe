// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Logging;
using Steeltoe.Discovery;
using Steeltoe.Discovery.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Extensions.Configuration.ConfigServer;

internal sealed class ConfigServerDiscoveryService
{
    internal IConfiguration Configuration;
    internal ConfigServerClientSettings Settings;
    internal ILoggerFactory LogFactory;
    internal ILogger Logger;
    internal IDiscoveryClient DiscoveryClient;
    private bool _usingInitialDiscoveryClient = true;

    internal ConfigServerDiscoveryService(IConfiguration configuration, ConfigServerClientSettings settings, ILoggerFactory logFactory = null)
    {
        this.Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.LogFactory = logFactory ?? BootstrapLoggerFactory.Instance;
        Logger = this.LogFactory.CreateLogger<ConfigServerDiscoveryService>();
        SetupDiscoveryClient();
    }

    // Create a discovery client to be used (hopefully only) during startup
    internal void SetupDiscoveryClient()
    {
        var services = new ServiceCollection();
        services.AddSingleton(LogFactory);
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

        // force settings to make sure we don't register the app here
        var cfgBuilder = new ConfigurationBuilder()
            .AddConfiguration(Configuration)
            .AddInMemoryCollection(
                new Dictionary<string, string>
                {
                    { "Eureka:Client:ShouldRegisterWithEureka", "false" },
                    { "Consul:Discovery:Register", "false" }
                });

        services.AddSingleton<IConfiguration>(cfgBuilder.Build());
        services.AddDiscoveryClient(Configuration);

        using var startupServiceProvider = services.BuildServiceProvider();
        DiscoveryClient = startupServiceProvider.GetRequiredService<IDiscoveryClient>();
        Logger.LogDebug("Found Discovery Client of type {DiscoveryClientType}", DiscoveryClient.GetType());
    }

    internal IEnumerable<IServiceInstance> GetConfigServerInstances()
    {
        var attempts = 0;
        var backOff = Settings.RetryInitialInterval;
        IEnumerable<IServiceInstance> instances;
        do
        {
            try
            {
                Logger.LogDebug("Locating configserver {serviceId} via discovery", Settings.DiscoveryServiceId);
                instances = DiscoveryClient.GetInstances(Settings.DiscoveryServiceId);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception invoking GetInstances() during config server lookup");
                instances = Enumerable.Empty<IServiceInstance>();
            }

            if (!Settings.RetryEnabled || instances.Any())
            {
                break;
            }

            attempts++;
            if (attempts <= Settings.RetryAttempts)
            {
                Thread.CurrentThread.Join(backOff);
                var nextBackoff = (int)(backOff * Settings.RetryMultiplier);
                backOff = Math.Min(nextBackoff, Settings.RetryMaxInterval);
            }
            else
            {
                break;
            }
        }
        while (true);

        return instances;
    }

    internal async Task ProvideRuntimeReplacementsAsync(IDiscoveryClient discoveryClientFromDI, ILoggerFactory loggerFactory)
    {
        if (discoveryClientFromDI is not null)
        {
            Logger.LogInformation("Replacing the IDiscoveryClient built at startup with one for runtime");
            await DiscoveryClient.ShutdownAsync().ConfigureAwait(false);
            DiscoveryClient = discoveryClientFromDI;
            _usingInitialDiscoveryClient = false;
        }
    }

    internal async Task ShutdownAsync()
    {
        if (_usingInitialDiscoveryClient)
        {
            await DiscoveryClient.ShutdownAsync();
        }
    }
}
