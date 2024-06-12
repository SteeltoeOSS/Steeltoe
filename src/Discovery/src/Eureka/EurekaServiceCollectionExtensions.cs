// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Certificate;
using Steeltoe.Common.Configuration;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Common.Net;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka;

public static class EurekaServiceCollectionExtensions
{
    private const string SpringDiscoveryEnabled = "spring:cloud:discovery:enabled";

    /// <summary>
    /// Configures to use <see cref="EurekaDiscoveryClient" /> for service discovery.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    public static IServiceCollection AddEurekaDiscoveryClient(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        if (services.All(descriptor => descriptor.ImplementationType != typeof(EurekaDiscoveryClient)))
        {
            ConfigureEurekaServices(services);
            AddEurekaServices(services);
        }

        return services;
    }

    private static void ConfigureEurekaServices(IServiceCollection services)
    {
        services.RegisterDefaultApplicationInstanceInfo();
        services.TryAddSingleton<InetUtils>();

        ConfigureEurekaClientOptions(services);
        ConfigureEurekaInstanceOptions(services);
    }

    private static void ConfigureEurekaClientOptions(IServiceCollection services)
    {
        services.ConfigureReloadableOptions<EurekaClientOptions>(EurekaClientOptions.ConfigurationPrefix, (options, serviceProvider) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            // Eureka is enabled by default. If eureka:client:enabled was not set then check spring:cloud:discovery:enabled
            if (options.Enabled && configuration.GetValue<bool?>($"{EurekaClientOptions.ConfigurationPrefix}:enabled") is null &&
                configuration.GetValue<bool?>(SpringDiscoveryEnabled) == false)
            {
                options.Enabled = false;
            }
        });

        services.AddSingleton<IValidateOptions<EurekaClientOptions>, ValidateEurekaClientOptions>();
    }

    private static void ConfigureEurekaInstanceOptions(IServiceCollection services)
    {
        services.ConfigureReloadableOptions<EurekaInstanceOptions>(EurekaInstanceOptions.ConfigurationPrefix);
        services.ConfigureReloadableOptions<InetOptions>(InetOptions.ConfigurationPrefix);
        services.AddSingleton<IPostConfigureOptions<EurekaInstanceOptions>, PostConfigureEurekaInstanceOptions>();

        DynamicPortAssignmentHostedService.Wire(services);
    }

    private static void AddEurekaServices(IServiceCollection services)
    {
        services.AddSingleton<HealthCheckHandlerProvider>();
        services.AddSingleton<IHealthContributor, EurekaServerHealthContributor>();
        services.AddSingleton<EurekaApplicationInfoManager>();

        services.TryAddSingleton<EurekaDiscoveryClient>();
        services.AddSingleton<IDiscoveryClient>(serviceProvider => serviceProvider.GetRequiredService<EurekaDiscoveryClient>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IHostedService), typeof(DiscoveryClientHostedService)));

        AddEurekaClient(services);

        services.TryAddSingleton<IHealthCheckHandler, EurekaHealthCheckHandler>();
        services.TryAddSingleton<IHealthAggregator, HealthRegistrationsAggregator>();
    }

    private static void AddEurekaClient(IServiceCollection services)
    {
        services.TryAddSingleton<HttpClientHandlerFactory>();
        services.TryAddSingleton<ValidateCertificatesHttpClientHandlerConfigurer<EurekaClientOptions>>();
        services.TryAddSingleton<ClientCertificateHttpClientHandlerConfigurer>();
        services.TryAddSingleton<EurekaHttpClientHandlerConfigurer>();
        services.ConfigureCertificateOptions("Eureka");

        IHttpClientBuilder eurekaHttpClientBuilder = services.AddHttpClient("Eureka");
        eurekaHttpClientBuilder.ConfigureAdditionalHttpMessageHandlers((defaultHandlers, _) => RemoveDiscoveryHttpDelegatingHandler(defaultHandlers));

        eurekaHttpClientBuilder.ConfigurePrimaryHttpMessageHandler(serviceProvider =>
        {
            var handlerFactory = serviceProvider.GetRequiredService<HttpClientHandlerFactory>();
            HttpClientHandler handler = handlerFactory.Create();

            var validateCertificatesHandler = serviceProvider.GetRequiredService<ValidateCertificatesHttpClientHandlerConfigurer<EurekaClientOptions>>();
            validateCertificatesHandler.Configure(handler);

            var clientCertificateConfigurer = serviceProvider.GetRequiredService<ClientCertificateHttpClientHandlerConfigurer>();
            clientCertificateConfigurer.SetCertificateName("Eureka");
            clientCertificateConfigurer.Configure(handler);

            var eurekaConfigurer = serviceProvider.GetRequiredService<EurekaHttpClientHandlerConfigurer>();
            eurekaConfigurer.Configure(handler);

            return handler;
        });

        IHttpClientBuilder eurekaTokenHttpClientBuilder = services.AddHttpClient("AccessTokenForEureka");
        eurekaTokenHttpClientBuilder.ConfigureAdditionalHttpMessageHandlers((defaultHandlers, _) => RemoveDiscoveryHttpDelegatingHandler(defaultHandlers));

        eurekaTokenHttpClientBuilder.ConfigurePrimaryHttpMessageHandler(serviceProvider =>
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

    private static void RemoveDiscoveryHttpDelegatingHandler(ICollection<DelegatingHandler> defaultHandlers)
    {
        DelegatingHandler[] discoveryHandlers = defaultHandlers.Where(handler =>
        {
            Type handlerType = handler.GetType();

            if (handlerType.IsConstructedGenericType)
            {
                Type handlerOpenType = handlerType.GetGenericTypeDefinition();

                if (handlerOpenType.FullName == "Steeltoe.Discovery.HttpClients.DiscoveryHttpDelegatingHandler`1")
                {
                    return true;
                }
            }

            return false;
        }).ToArray();

        foreach (DelegatingHandler discoveryHandler in discoveryHandlers)
        {
            // Prevent infinite recursion: DiscoveryHttpDelegatingHandler depends on EurekaDiscoveryClient.
            defaultHandlers.Remove(discoveryHandler);
        }
    }
}
