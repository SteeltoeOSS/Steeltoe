// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
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
using Steeltoe.Connectors.Services;
using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Eureka.Transport;
using static Steeltoe.Discovery.Client.DiscoveryServiceCollectionExtensions;

namespace Steeltoe.Discovery.Eureka;

public class EurekaDiscoveryClientExtension : IDiscoveryClientExtension
{
    private const string SpringDiscoveryEnabled = "spring:cloud:discovery:enabled";
    public const string EurekaPrefix = "eureka";

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
        services.AddOptions<EurekaClientOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            configuration.GetSection(EurekaClientOptions.EurekaClientConfigurationPrefix).Bind(options);

            // Eureka is enabled by default. If eureka:client:enabled was not set then check spring:cloud:discovery:enabled
            if (options.Enabled && configuration.GetValue<bool?>($"{EurekaClientOptions.EurekaClientConfigurationPrefix}:enabled") is null &&
                configuration.GetValue<bool?>(SpringDiscoveryEnabled) == false)
            {
                options.Enabled = false;
            }
        }).PostConfigure<IConfiguration>((options, configuration) =>
        {
            EurekaServiceInfo info = GetServiceInfo(configuration);
            EurekaPostConfigurer.UpdateConfiguration(info, options);
        });

        services.AddOptions<EurekaInstanceOptions>()
            .Configure<IConfiguration>((options, configuration) =>
                configuration.GetSection(EurekaInstanceOptions.EurekaInstanceConfigurationPrefix).Bind(options)).PostConfigure<IServiceProvider>(
                (options, serviceProvider) =>
                {
                    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                    var appInfo = serviceProvider.GetRequiredService<IApplicationInstanceInfo>();
                    var inetOptions = configuration.GetSection(InetOptions.Prefix).Get<InetOptions>();
                    options.NetUtils = new InetUtils(inetOptions);
                    options.ApplyNetUtils();

                    if (ReflectionHelpers.IsAssemblyLoaded("Steeltoe.Management.Endpoint") && string.IsNullOrEmpty(
                        configuration.GetValue<string>($"{EurekaInstanceOptions.EurekaInstanceConfigurationPrefix}:HealthCheckUrlPath")))
                    {
                        GetPathsFromEndpointOptions(options, serviceProvider, configuration);
                    }

                    EurekaServiceInfo info = GetServiceInfo(configuration);
                    EurekaPostConfigurer.UpdateConfiguration(configuration, info, options, info?.ApplicationInfo ?? appInfo);
                });

        services.TryAddSingleton(serviceProvider =>
        {
            var clientOptions = serviceProvider.GetRequiredService<IOptions<EurekaClientOptions>>();

            return new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(clientOptions.Value.CacheTtl)
            };
        });
    }

    private static void GetPathsFromEndpointOptions(EurekaInstanceOptions options, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        const string endpointAssembly = "Steeltoe.Management.Endpoint";

        Type managementOptionsType = ReflectionHelpers.FindType(new[]
        {
            endpointAssembly
        }, new[]
        {
            "Steeltoe.Management.Endpoint.Options.ManagementOptions"
        });

        Type endpointOptionsType = ReflectionHelpers.FindType(new[]
        {
            "Steeltoe.Management.Abstractions"
        }, new[]
        {
            "Steeltoe.Management.EndpointOptions"
        });

        object actuatorOptions = GetOptionsMonitor(serviceProvider, managementOptionsType);
        string basePath = $"{(string)actuatorOptions.GetType().GetProperty("Path")?.GetValue(actuatorOptions)}/";

        object healthOptions = ConfigureOptionsType(serviceProvider, configuration, endpointAssembly,
            "Steeltoe.Management.Endpoint.Health.ConfigureHealthEndpointOptions", "Steeltoe.Management.Endpoint.Health.HealthEndpointOptions");

        if (healthOptions != null)
        {
            options.HealthCheckUrlPath = basePath + ((string)endpointOptionsType.GetProperty("Path")?.GetValue(healthOptions))?.TrimStart('/');
        }

        if (string.IsNullOrEmpty(configuration.GetValue<string>($"{EurekaInstanceOptions.EurekaInstanceConfigurationPrefix}:StatusPageUrlPath")))
        {
            object infoOptions = ConfigureOptionsType(serviceProvider, configuration, endpointAssembly,
                "Steeltoe.Management.Endpoint.Info.ConfigureInfoEndpointOptions", "Steeltoe.Management.Endpoint.Info.InfoEndpointOptions");

            if (infoOptions != null)
            {
                options.StatusPageUrlPath = basePath + ((string)endpointOptionsType.GetProperty("Path")?.GetValue(infoOptions))?.TrimStart('/');
            }
        }
    }

    private static object ConfigureOptionsType(IServiceProvider serviceProvider, IConfiguration configuration, string endpointAssembly,
        string configureOptionsTypeName, string optionsTypeName)
    {
        Type configureOptionsType = ReflectionHelpers.FindType(new[]
        {
            endpointAssembly
        }, new[]
        {
            configureOptionsTypeName
        });

        Type optionsType = ReflectionHelpers.FindType(new[]
        {
            endpointAssembly
        }, new[]
        {
            optionsTypeName
        });

        object configureOptions = Activator.CreateInstance(configureOptionsType, configuration);

        object options = GetOptionsMonitor(serviceProvider, optionsType);
        MethodInfo methodInfo = configureOptionsType.GetMethod("Configure");

        methodInfo.Invoke(configureOptions, new[]
        {
            options
        });

        return options;
    }

    private static object GetOptionsMonitor(IServiceProvider serviceProvider, Type optionsType)
    {
        Type optionsMonitorType = typeof(IOptionsMonitor<>);
        Type genericOptionsType = optionsMonitorType.MakeGenericType(optionsType);
        object optionsMonitor = serviceProvider.GetService(genericOptionsType);

        return genericOptionsType.GetProperty("CurrentValue")?.GetValue(optionsMonitor);
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

        ServiceDescriptor existingHandler = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IHttpClientHandlerProvider));

        if (existingHandler != null && typeof(IHttpClientHandlerProvider).IsAssignableFrom(existingHandler.ImplementationType))
        {
            AddEurekaHttpClient(services)
                .ConfigurePrimaryHttpMessageHandler(serviceProvider => serviceProvider.GetService<IHttpClientHandlerProvider>().GetHttpClientHandler());
        }
        else
        {
            AddEurekaHttpClient(services).ConfigurePrimaryHttpMessageHandler(serviceProvider =>
            {
                var certOptions = serviceProvider.GetService<IOptionsMonitor<CertificateOptions>>();
                var eurekaOptions = serviceProvider.GetService<IOptionsMonitor<EurekaClientOptions>>();

                return EurekaHttpClient.ConfigureEurekaHttpClientHandler(eurekaOptions.CurrentValue,
                    certOptions is null ? null : new ClientCertificateHttpHandler(certOptions));
            });
        }
    }

    private IHttpClientBuilder AddEurekaHttpClient(IServiceCollection services)
    {
        return services.AddHttpClient<EurekaDiscoveryClient>("Eureka", (services, client) =>
        {
            var clientOptions = services.GetRequiredService<IOptions<EurekaClientOptions>>();

            if (clientOptions.Value.EurekaServerConnectTimeoutSeconds > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(clientOptions.Value.EurekaServerConnectTimeoutSeconds);
            }
        });
    }

    private EurekaServiceInfo GetServiceInfo(IConfiguration configuration)
    {
        ServiceInfoName ??= configuration.GetValue<string>("eureka:serviceInfoName");

        IServiceInfo info = string.IsNullOrEmpty(ServiceInfoName)
            ? GetSingletonDiscoveryServiceInfo(configuration)
            : GetNamedDiscoveryServiceInfo(configuration, ServiceInfoName);

        return info as EurekaServiceInfo;
    }
}
