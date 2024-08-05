// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common.Http.HttpClientPooling;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

public static class CloudFoundrySecurityServiceCollectionExtensions
{
    public static IServiceCollection AddCloudFoundrySecurity(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<PermissionsProvider>();
        ConfigureHttpClient(services);

        return services;
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

            validateCertificatesHandler.Configure(handler);

            return handler;
        });
    }
}
