// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Common.Net;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register startup/shutdown interactions with Spring Boot Admin server.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddSpringBootAdminClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions();
        services.ConfigureOptionsWithChangeTokenSource<ManagementOptions, ConfigureManagementOptions>();
        services.ConfigureOptionsWithChangeTokenSource<SpringBootAdminClientOptions, ConfigureSpringBootAdminClientOptions>();
        services.ConfigureEndpointOptions<HealthEndpointOptions, ConfigureHealthEndpointOptions>();

        ConfigureHttpClient(services);

        services.AddApplicationInstanceInfo();
        services.TryAddSingleton<IDomainNameResolver>(DomainNameResolver.Instance);
        services.TryAddSingleton<InetUtils>();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<AppUrlCalculator>();
        services.TryAddSingleton<SpringBootAdminRefreshRunner>();
        services.AddHostedService<SpringBootAdminClientHostedService>();

        return services;
    }

    private static void ConfigureHttpClient(IServiceCollection services)
    {
        services.TryAddSingleton<HttpClientHandlerFactory>();
        services.TryAddSingleton<ValidateCertificatesHttpClientHandlerConfigurer<SpringBootAdminClientOptions>>();

        IHttpClientBuilder httpClientBuilder = services.AddHttpClient(SpringBootAdminApiClient.HttpClientName);

        httpClientBuilder.ConfigurePrimaryHttpMessageHandler(serviceProvider =>
        {
            var handlerFactory = serviceProvider.GetRequiredService<HttpClientHandlerFactory>();
            HttpClientHandler handler = handlerFactory.Create();

            var validateCertificatesHandler =
                serviceProvider.GetRequiredService<ValidateCertificatesHttpClientHandlerConfigurer<SpringBootAdminClientOptions>>();

            validateCertificatesHandler.Configure(Options.DefaultName, handler);

            return handler;
        });

        services.TryAddSingleton<SpringBootAdminApiClient>();
    }
}
