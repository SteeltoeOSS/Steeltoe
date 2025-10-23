// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Certificates;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Common.Net;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka;

public static class EurekaServiceCollectionExtensions
{
    private const string SpringDiscoveryEnabled = "spring:cloud:discovery:enabled";
    private const string ResolvingHttpDelegatingHandlerName = "Microsoft.Extensions.ServiceDiscovery.Http.ResolvingHttpDelegatingHandler";

    /// <summary>
    /// Configures to use <see cref="EurekaDiscoveryClient" /> for service discovery.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddEurekaDiscoveryClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (!IsRegistered(services))
        {
            ConfigureEurekaServices(services);
            AddEurekaServices(services);
        }

        return services;
    }

    private static bool IsRegistered(IServiceCollection services)
    {
        return services.Any(descriptor => descriptor.SafeGetImplementationType() == typeof(EurekaDiscoveryClient));
    }

    private static void ConfigureEurekaServices(IServiceCollection services)
    {
        services.AddApplicationInstanceInfo();
        services.TryAddSingleton<IDomainNameResolver>(DomainNameResolver.Instance);
        services.TryAddSingleton<InetUtils>();

        ConfigureEurekaClientOptions(services);
        ConfigureEurekaInstanceOptions(services);
    }

    private static void ConfigureEurekaClientOptions(IServiceCollection services)
    {
        OptionsBuilder<EurekaClientOptions> optionsBuilder = services.AddOptions<EurekaClientOptions>();
        optionsBuilder.BindConfiguration(EurekaClientOptions.ConfigurationPrefix);

        optionsBuilder.Configure<IServiceProvider>((options, serviceProvider) =>
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
        services.AddOptions<EurekaInstanceOptions>().BindConfiguration(EurekaInstanceOptions.ConfigurationPrefix);
        services.AddOptions<InetOptions>().BindConfiguration(InetOptions.ConfigurationPrefix);
        services.AddSingleton<IPostConfigureOptions<EurekaInstanceOptions>, PostConfigureEurekaInstanceOptions>();

        DynamicPortAssignmentHostedService.Wire(services);
    }

    private static void AddEurekaServices(IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<HealthCheckHandlerProvider>();
        services.AddSingleton<IHealthContributor, EurekaServerHealthContributor>();
        services.TryAddSingleton<EurekaApplicationInfoManager>();

        services.TryAddSingleton<EurekaDiscoveryClient>();
        services.AddSingleton<IDiscoveryClient>(serviceProvider => serviceProvider.GetRequiredService<EurekaDiscoveryClient>());
        services.AddHostedService<DiscoveryClientHostedService>();

        AddEurekaClient(services);

        services.TryAddSingleton<IHealthCheckHandler, EurekaHealthCheckHandler>();
        services.TryAddSingleton<IHealthAggregator, HealthAggregator>();
    }

    private static void AddEurekaClient(IServiceCollection services)
    {
        services.TryAddSingleton<HttpClientHandlerFactory>();
        services.AddSingleton<ValidateCertificatesHttpClientHandlerConfigurer<EurekaClientOptions>>();
        services.TryAddSingleton<ClientCertificateHttpClientHandlerConfigurer>();
        services.AddSingleton<EurekaHttpClientHandlerConfigurer>();
        services.ConfigureCertificateOptions("Eureka");

        IHttpClientBuilder eurekaHttpClientBuilder = services.AddHttpClient("Eureka");
        eurekaHttpClientBuilder.ConfigureAdditionalHttpMessageHandlers((defaultHandlers, _) => RemoveDiscoveryHttpHandlers(defaultHandlers));

        eurekaHttpClientBuilder.ConfigurePrimaryHttpMessageHandler(serviceProvider =>
        {
            var handlerFactory = serviceProvider.GetRequiredService<HttpClientHandlerFactory>();
            HttpClientHandler handler = handlerFactory.Create();

            var eurekaConfigurer = serviceProvider.GetRequiredService<EurekaHttpClientHandlerConfigurer>();
            eurekaConfigurer.Configure(handler);

            return handler;
        });

        IHttpClientBuilder eurekaTokenHttpClientBuilder = services.AddHttpClient("AccessTokenForEureka");
        eurekaTokenHttpClientBuilder.ConfigureAdditionalHttpMessageHandlers((defaultHandlers, _) => RemoveDiscoveryHttpHandlers(defaultHandlers));

        eurekaTokenHttpClientBuilder.ConfigurePrimaryHttpMessageHandler(serviceProvider =>
        {
            var handlerFactory = serviceProvider.GetRequiredService<HttpClientHandlerFactory>();
            HttpClientHandler handler = handlerFactory.Create();

            var validateCertificatesConfigurer = serviceProvider.GetRequiredService<ValidateCertificatesHttpClientHandlerConfigurer<EurekaClientOptions>>();
            validateCertificatesConfigurer.Configure(Options.DefaultName, handler);

            return handler;
        });

        services.AddSingleton<EurekaServiceUriStateManager>();
        services.AddSingleton<EurekaClient>();
    }

    private static void RemoveDiscoveryHttpHandlers(ICollection<DelegatingHandler> defaultHandlers)
    {
        // Prevent infinite recursion: The inner HttClient used by EurekaDiscoveryClient must not use service discovery.

        DelegatingHandler[] discoveryHandlers = defaultHandlers.Where(handler =>
        {
            Type handlerType = handler.GetType();

            if (handlerType.FullName == ResolvingHttpDelegatingHandlerName)
            {
                return true;
            }

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
            defaultHandlers.Remove(discoveryHandler);
        }
    }
}
