// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register startup/shutdown interactions with Spring Boot Admin server.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddSpringBootAdminClient(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.RegisterDefaultApplicationInstanceInfo();

        // Workaround for services.ConfigureOptions<ConfigureManagementOptions>() registering multiple times,
        // see https://github.com/dotnet/runtime/issues/42358.
        services.AddOptions();
        services.TryAddTransient<IConfigureOptions<ManagementOptions>, ConfigureManagementOptions>();

        services.ConfigureEndpointOptions<HealthEndpointOptions, ConfigureHealthEndpointOptions>();
        services.ConfigureOptionsWithChangeTokenSource<SpringBootAdminClientOptions, ConfigureSpringBootAdminClientOptions>();

        ConfigureHttpClient(services);

        services.AddHostedService<SpringBootAdminClientHostedService>();
        return services;
    }

    private static void ConfigureHttpClient(IServiceCollection services)
    {
        services.TryAddSingleton<HttpClientHandlerFactory>();
        services.TryAddSingleton<ValidateCertificatesHttpClientHandlerConfigurer<SpringBootAdminClientOptions>>();

        IHttpClientBuilder httpClientBuilder = services.AddHttpClient(SpringBootAdminClientHostedService.HttpClientName);

        httpClientBuilder.ConfigurePrimaryHttpMessageHandler(serviceProvider =>
        {
            var handlerFactory = serviceProvider.GetRequiredService<HttpClientHandlerFactory>();
            HttpClientHandler handler = handlerFactory.Create();

            var validateCertificatesHandler =
                serviceProvider.GetRequiredService<ValidateCertificatesHttpClientHandlerConfigurer<SpringBootAdminClientOptions>>();

            validateCertificatesHandler.Configure(handler);

            return handler;
        });
    }
}
