// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Http;
using Steeltoe.Common.Net;
using Steeltoe.Common.Options;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Eureka.Transport;
using System;
using System.Linq;
using static Steeltoe.Discovery.Client.DiscoveryServiceCollectionExtensions;

namespace Steeltoe.Discovery.Eureka;

public class EurekaDiscoveryClientExtension : IDiscoveryClientExtension
{
    public const string EurekaPrefix = "eureka";
    private const string SpringDiscoveryEnabled = "spring:cloud:discovery:enabled";

    public string ServiceInfoName { get; private set; }

    public EurekaDiscoveryClientExtension()
        : this(null)
    {
    }

    public EurekaDiscoveryClientExtension(string serviceInfoName)
    {
        ServiceInfoName = serviceInfoName;
    }

    /// <inheritdoc />
    public void ApplyServices(IServiceCollection services)
    {
        ConfigureEurekaServices(services);
        AddEurekaServices(services);
    }

    public bool IsConfigured(IConfiguration configuration, IServiceInfo serviceInfo = null)
    {
        return configuration.GetSection(EurekaPrefix).GetChildren().Any() || serviceInfo is EurekaServiceInfo;
    }

    internal void ConfigureEurekaServices(IServiceCollection services)
    {
        services
            .AddOptions<EurekaClientOptions>()
            .Configure<IConfiguration>((options, config) =>
            {
                config.GetSection(EurekaClientOptions.EurekaClientConfigurationPrefix).Bind(options);

                // Eureka is enabled by default. If eureka:client:enabled was not set then check spring:cloud:discovery:enabled
                if (options.Enabled &&
                    config.GetValue<bool?>($"{EurekaClientOptions.EurekaClientConfigurationPrefix}:enabled") is null &&
                    config.GetValue<bool?>(SpringDiscoveryEnabled) == false)
                {
                    options.Enabled = false;
                }
            })
            .PostConfigure<IConfiguration>((options, config) =>
            {
                var info = GetServiceInfo(config);
                EurekaPostConfigurer.UpdateConfiguration(config, info, options);
            });

        services
            .AddOptions<EurekaInstanceOptions>()
            .Configure<IConfiguration>((options, config) => config.GetSection(EurekaInstanceOptions.EurekaInstanceConfigurationPrefix).Bind(options))
            .PostConfigure<IServiceProvider>((options, serviceProvider) =>
            {
                var config = serviceProvider.GetRequiredService<IConfiguration>();
                var appInfo = serviceProvider.GetRequiredService<IApplicationInstanceInfo>();
                var inetOptions = config.GetSection(InetOptions.Prefix).Get<InetOptions>();
                options.NetUtils = new InetUtils(inetOptions);
                options.ApplyNetUtils();
                const string endpointAssembly = "Steeltoe.Management.EndpointBase";
                if (ReflectionHelpers.IsAssemblyLoaded(endpointAssembly))
                {
                    var actuatorOptionsType = ReflectionHelpers.FindType(new[] { endpointAssembly }, new[] { "Steeltoe.Management.Endpoint.Hypermedia.ActuatorManagementOptions" });
                    var endpointOptionsBaseType = ReflectionHelpers.FindType(new[] { "Steeltoe.Management.Abstractions" }, new[] { "Steeltoe.Management.IEndpointOptions" });
                    var managementOptions = serviceProvider.GetService(actuatorOptionsType);
                    if (managementOptions != null)
                    {
                        var basePath = $"{(string)actuatorOptionsType.GetProperty("Path").GetValue(managementOptions)}/";
                        if (string.IsNullOrEmpty(config.GetValue<string>($"{EurekaInstanceOptions.EurekaInstanceConfigurationPrefix}:HealthCheckUrlPath")))
                        {
                            var healthOptionsType = ReflectionHelpers.FindType(new[] { endpointAssembly }, new[] { "Steeltoe.Management.Endpoint.Health.IHealthOptions" });
                            var healthOptions = serviceProvider.GetService(healthOptionsType);
                            if (healthOptions != null)
                            {
                                options.HealthCheckUrlPath = basePath + ((string)endpointOptionsBaseType.GetProperty("Path")?.GetValue(healthOptions))?.TrimStart('/');
                            }
                        }

                        if (string.IsNullOrEmpty(config.GetValue<string>($"{EurekaInstanceOptions.EurekaInstanceConfigurationPrefix}:StatusPageUrlPath")))
                        {
                            var infoOptionsType = ReflectionHelpers.FindType(new[] { endpointAssembly }, new[] { "Steeltoe.Management.Endpoint.Info.IInfoOptions" });
                            var infoOptions = serviceProvider.GetService(infoOptionsType);
                            if (infoOptions != null)
                            {
                                options.StatusPageUrlPath = basePath + ((string)endpointOptionsBaseType.GetProperty("Path")?.GetValue(infoOptions))?.TrimStart('/');
                            }
                        }
                    }
                }

                var info = GetServiceInfo(config);
                EurekaPostConfigurer.UpdateConfiguration(config, info, options, info?.ApplicationInfo ?? appInfo);
            });

        services.TryAddSingleton(serviceProvider =>
        {
            var clientOptions = serviceProvider.GetRequiredService<IOptions<EurekaClientOptions>>();
            return new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(clientOptions.Value.CacheTtl) };
        });
    }

    private void AddEurekaServices(IServiceCollection services)
    {
        services.AddSingleton<EurekaApplicationInfoManager>();
        services.AddSingleton<EurekaDiscoveryManager>();
        services.AddSingleton<EurekaDiscoveryClient>();
        services.AddSingleton<IDiscoveryClient>(p =>
        {
            var eurekaService = p.GetService<EurekaDiscoveryClient>();

            // Wire in health checker if present
            if (eurekaService != null)
            {
                eurekaService.HealthCheckHandler = p.GetService<IHealthCheckHandler>();
            }

            return eurekaService;
        });

        services.AddSingleton<IHealthContributor, EurekaServerHealthContributor>();

        var existingHandler = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IHttpClientHandlerProvider));

        if (existingHandler is IHttpClientHandlerProvider handlerProvider)
        {
            AddEurekaHttpClient(services)
                .ConfigurePrimaryHttpMessageHandler(() => handlerProvider.GetHttpClientHandler());
        }
        else
        {
            AddEurekaHttpClient(services)
                .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
                {
                    var certOptions = serviceProvider.GetService<IOptionsMonitor<CertificateOptions>>();
                    var eurekaOptions = serviceProvider.GetService<IOptionsMonitor<EurekaClientOptions>>();
                    return EurekaHttpClient.ConfigureEurekaHttpClientHandler(eurekaOptions.CurrentValue, certOptions is null ? null : new ClientCertificateHttpHandler(certOptions));
                });
        }
    }

    private IHttpClientBuilder AddEurekaHttpClient(IServiceCollection services)
        => services.AddHttpClient<EurekaDiscoveryClient>("Eureka", (services, client) =>
        {
            var clientOptions = services.GetRequiredService<IOptions<EurekaClientOptions>>();
            if (clientOptions.Value.EurekaServerConnectTimeoutSeconds > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(clientOptions.Value.EurekaServerConnectTimeoutSeconds);
            }
        });

    private EurekaServiceInfo GetServiceInfo(IConfiguration config)
    {
        ServiceInfoName ??= config.GetValue<string>("eureka:serviceInfoName");
        var info = string.IsNullOrEmpty(ServiceInfoName)
            ? GetSingletonDiscoveryServiceInfo(config)
            : GetNamedDiscoveryServiceInfo(config, ServiceInfoName);

        return info as EurekaServiceInfo;
    }
}
