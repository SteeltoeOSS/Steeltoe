// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Common.Net;
using Steeltoe.Common.Reflection;
using Steeltoe.Connectors.Services;
using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Eureka.Configuration;
using static Steeltoe.Discovery.Client.DiscoveryServiceCollectionExtensions;

namespace Steeltoe.Discovery.Eureka;

internal sealed class EurekaDiscoveryClientExtension : IDiscoveryClientExtension
{
    private const string SpringDiscoveryEnabled = "spring:cloud:discovery:enabled";
    private const string EurekaPrefix = "eureka";

    private readonly string? _serviceInfoName;

    public EurekaDiscoveryClientExtension()
        : this(null)
    {
    }

    public EurekaDiscoveryClientExtension(string? serviceInfoName)
    {
        _serviceInfoName = serviceInfoName;
    }

    /// <inheritdoc />
    public bool IsConfigured(IConfiguration configuration, IServiceInfo? serviceInfo)
    {
        return configuration.GetSection(EurekaPrefix).GetChildren().Any() || serviceInfo is EurekaServiceInfo;
    }

    /// <inheritdoc />
    public void ApplyServices(IServiceCollection services)
    {
        ConfigureEurekaServices(services);
        AddEurekaServices(services);
    }

    internal void ConfigureEurekaServices(IServiceCollection services)
    {
        SetupConfigurationChangeDetection<EurekaClientOptions>(services, EurekaClientOptions.ConfigurationPrefix);
        SetupConfigurationChangeDetection<EurekaInstanceOptions>(services, EurekaInstanceOptions.ConfigurationPrefix);

        services.AddOptions<EurekaClientOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            configuration.GetSection(EurekaClientOptions.ConfigurationPrefix).Bind(options);

            // Eureka is enabled by default. If eureka:client:enabled was not set then check spring:cloud:discovery:enabled
            if (options.Enabled && configuration.GetValue<bool?>($"{EurekaClientOptions.ConfigurationPrefix}:enabled") is null &&
                configuration.GetValue<bool?>(SpringDiscoveryEnabled) == false)
            {
                options.Enabled = false;
            }
        }).PostConfigure<IConfiguration>((options, configuration) =>
        {
            EurekaServiceInfo? info = GetServiceInfo(configuration);
            EurekaPostConfigurer.UpdateConfiguration(info, options);
        });

        services.AddOptions<EurekaInstanceOptions>()
            .Configure<IConfiguration>((options, configuration) => configuration.GetSection(EurekaInstanceOptions.ConfigurationPrefix).Bind(options))
            .PostConfigure<IServiceProvider>((options, serviceProvider) =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var appInfo = serviceProvider.GetRequiredService<IApplicationInstanceInfo>();
                InetOptions inetOptions = configuration.GetSection(InetOptions.ConfigurationPrefix).Get<InetOptions>() ?? new InetOptions();
                var logger = serviceProvider.GetRequiredService<ILogger<InetUtils>>();
                options.NetUtils = new InetUtils(inetOptions, logger);
                options.ApplyNetUtils();

                if (ReflectionHelpers.IsAssemblyLoaded("Steeltoe.Management.Endpoint") && string.IsNullOrEmpty(
                    configuration.GetValue<string>($"{EurekaInstanceOptions.ConfigurationPrefix}:HealthCheckUrlPath")))
                {
                    GetPathsFromEndpointOptions(options, serviceProvider, configuration);
                }

                EurekaServiceInfo? serviceInfo = GetServiceInfo(configuration);
                EurekaPostConfigurer.UpdateConfiguration(configuration, serviceInfo, options, serviceInfo?.ApplicationInfo ?? appInfo);
            });
    }

    private static void SetupConfigurationChangeDetection<TOptions>(IServiceCollection services, string configurationKey)
        where TOptions : class
    {
        services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            return new ConfigurationChangeTokenSource<TOptions>(configuration.GetSection(configurationKey));
        });
    }

    private static void GetPathsFromEndpointOptions(EurekaInstanceOptions eurekaOptions, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        const string endpointAssembly = "Steeltoe.Management.Endpoint";

        Type managementOptionsType = ReflectionHelpers.FindType([endpointAssembly], ["Steeltoe.Management.Endpoint.Options.ManagementOptions"]);

        object actuatorOptionsMonitor = GetOptionsFromMonitor(serviceProvider, managementOptionsType);
        PropertyInfo managementOptionsPathProperty = actuatorOptionsMonitor.GetType().GetProperty("Path")!;
        string basePath = $"{managementOptionsPathProperty.GetValue(actuatorOptionsMonitor)}/";

        object healthOptions = ConfigureOptionsType(serviceProvider, configuration, endpointAssembly,
            "Steeltoe.Management.Endpoint.Health.ConfigureHealthEndpointOptions", "Steeltoe.Management.Endpoint.Health.HealthEndpointOptions");

        Type endpointOptionsType = ReflectionHelpers.FindType(["Steeltoe.Management.Abstractions"], ["Steeltoe.Management.EndpointOptions"]);
        PropertyInfo endpointOptionsPathProperty = endpointOptionsType.GetProperty("Path")!;

        eurekaOptions.HealthCheckUrlPath = basePath + ((string?)endpointOptionsPathProperty.GetValue(healthOptions))?.TrimStart('/');

        if (string.IsNullOrEmpty(configuration.GetValue<string>($"{EurekaInstanceOptions.ConfigurationPrefix}:StatusPageUrlPath")))
        {
            object infoOptions = ConfigureOptionsType(serviceProvider, configuration, endpointAssembly,
                "Steeltoe.Management.Endpoint.Info.ConfigureInfoEndpointOptions", "Steeltoe.Management.Endpoint.Info.InfoEndpointOptions");

            eurekaOptions.StatusPageUrlPath = basePath + ((string?)endpointOptionsPathProperty.GetValue(infoOptions))?.TrimStart('/');
        }
    }

    private static object ConfigureOptionsType(IServiceProvider serviceProvider, IConfiguration configuration, string endpointAssembly,
        string configureOptionsTypeName, string optionsTypeName)
    {
        Type configureOptionsType = ReflectionHelpers.FindType([endpointAssembly], [configureOptionsTypeName]);
        Type optionsType = ReflectionHelpers.FindType([endpointAssembly], [optionsTypeName]);

        object configureOptionsInstance = Activator.CreateInstance(configureOptionsType, configuration)!;
        object optionsInstance = GetOptionsFromMonitor(serviceProvider, optionsType);

        MethodInfo configureMethod = configureOptionsType.GetMethod(nameof(IConfigureOptions<object>.Configure))!;
        configureMethod.Invoke(configureOptionsInstance, [optionsInstance]);

        return optionsInstance;
    }

    private static object GetOptionsFromMonitor(IServiceProvider serviceProvider, Type optionsType)
    {
        Type optionsMonitorType = typeof(IOptionsMonitor<>).MakeGenericType(optionsType);
        PropertyInfo currentValueProperty = optionsMonitorType.GetProperty(nameof(IOptionsMonitor<object>.CurrentValue))!;

        object optionsMonitorInstance = serviceProvider.GetRequiredService(optionsMonitorType);
        return currentValueProperty.GetValue(optionsMonitorInstance)!;
    }

    private void AddEurekaServices(IServiceCollection services)
    {
        services.AddSingleton<EurekaApplicationInfoManager>();
        services.AddSingleton<EurekaDiscoveryClient>();

        services.AddSingleton<IDiscoveryClient>(serviceProvider =>
        {
            var eurekaDiscoveryClient = serviceProvider.GetRequiredService<EurekaDiscoveryClient>();

            // Wire in health checker if present
            eurekaDiscoveryClient.HealthCheckHandler = serviceProvider.GetService<IHealthCheckHandler>();

            return eurekaDiscoveryClient;
        });

        services.AddSingleton<IHealthContributor, EurekaServerHealthContributor>();

        AddEurekaClient(services);
    }

    private static void AddEurekaClient(IServiceCollection services)
    {
        services.TryAddSingleton<HttpClientHandlerFactory>();
        services.TryAddSingleton<ValidateCertificatesHttpClientHandlerConfigurer<EurekaClientOptions>>();
        services.TryAddSingleton<ClientCertificateHttpClientHandlerConfigurer>();
        services.TryAddSingleton<EurekaHttpClientHandlerConfigurer>();

        services.AddHttpClient("Eureka").ConfigurePrimaryHttpMessageHandler(serviceProvider =>
        {
            var handlerFactory = serviceProvider.GetRequiredService<HttpClientHandlerFactory>();
            HttpClientHandler handler = handlerFactory.Create();

            var validateCertificatesHandler = serviceProvider.GetRequiredService<ValidateCertificatesHttpClientHandlerConfigurer<EurekaClientOptions>>();
            validateCertificatesHandler.Configure(handler);

            var clientCertificateConfigurer = serviceProvider.GetRequiredService<ClientCertificateHttpClientHandlerConfigurer>();
            clientCertificateConfigurer.Configure(handler);

            var eurekaConfigurer = serviceProvider.GetRequiredService<EurekaHttpClientHandlerConfigurer>();
            eurekaConfigurer.Configure(handler);

            return handler;
        });

        services.AddHttpClient("AccessTokenForEureka").ConfigurePrimaryHttpMessageHandler(serviceProvider =>
        {
            var handlerFactory = serviceProvider.GetRequiredService<HttpClientHandlerFactory>();
            HttpClientHandler handler = handlerFactory.Create();

            var validateCertificatesHandler = serviceProvider.GetRequiredService<ValidateCertificatesHttpClientHandlerConfigurer<EurekaClientOptions>>();
            validateCertificatesHandler.Configure(handler);

            return handler;
        });

        services.AddSingleton<EurekaServiceUriStateManager>();
        services.AddSingleton<EurekaClient>();
    }

    private EurekaServiceInfo? GetServiceInfo(IConfiguration configuration)
    {
        string? serviceInfoName = _serviceInfoName ?? configuration.GetValue<string>("eureka:serviceInfoName");

        IServiceInfo? info = string.IsNullOrEmpty(serviceInfoName)
            ? GetSingleDiscoveryServiceInfo(configuration)
            : GetNamedDiscoveryServiceInfo(configuration, serviceInfoName);

        return info as EurekaServiceInfo;
    }
}
