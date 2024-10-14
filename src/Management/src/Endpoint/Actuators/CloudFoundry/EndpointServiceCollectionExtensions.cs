// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;

namespace Steeltoe.Management.Endpoint.Actuators.CloudFoundry;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Cloud Foundry actuator to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddCloudFoundryActuator(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddCoreActuatorServicesAsSingleton<CloudFoundryEndpointOptions, ConfigureCloudFoundryEndpointOptions, CloudFoundryEndpointMiddleware,
                ICloudFoundryEndpointHandler, CloudFoundryEndpointHandler, string, Links>();

        AddCloudFoundrySecurity(services);

        return services;
    }

    private static void AddCloudFoundrySecurity(IServiceCollection services)
    {
        services.AddSingleton<PermissionsProvider>();
        ConfigureHttpClient(services);
    }

    private static void ConfigureHttpClient(IServiceCollection services)
    {
        services.TryAddSingleton<HttpClientHandlerFactory>();
        services.TryAddSingleton<ValidateCertificatesHttpClientHandlerConfigurer<CloudFoundryEndpointOptions>>();

        IHttpClientBuilder httpClientBuilder = services.AddHttpClient(PermissionsProvider.HttpClientName);

        httpClientBuilder.ConfigurePrimaryHttpMessageHandler(serviceProvider =>
        {
            var handlerFactory = serviceProvider.GetRequiredService<HttpClientHandlerFactory>();
            HttpClientHandler handler = handlerFactory.Create();

            var validateCertificatesHandler =
                serviceProvider.GetRequiredService<ValidateCertificatesHttpClientHandlerConfigurer<CloudFoundryEndpointOptions>>();

            validateCertificatesHandler.Configure(Options.DefaultName, handler);

            return handler;
        });
    }
}
