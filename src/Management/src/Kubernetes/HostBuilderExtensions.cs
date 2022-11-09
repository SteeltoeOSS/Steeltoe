// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint;

namespace Steeltoe.Management.Kubernetes;

public static class HostBuilderExtensions
{
    /// <summary>
    /// Adds all standard and Kubernetes-specific actuators to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <param name="mediaTypeVersion">
    /// Specify the media type version to use in the response.
    /// </param>
    public static IHostBuilder AddKubernetesActuators(this IHostBuilder hostBuilder, MediaTypeVersion mediaTypeVersion)
    {
        return hostBuilder.AddKubernetesActuators(null, mediaTypeVersion);
    }

    /// <summary>
    /// Adds all standard and Kubernetes-specific actuators to the application.
    /// </summary>
    /// <param name="webHostBuilder">
    /// Your WebHostBuilder.
    /// </param>
    /// <param name="mediaTypeVersion">
    /// Specify the media type version to use in the response.
    /// </param>
    public static IWebHostBuilder AddKubernetesActuators(this IWebHostBuilder webHostBuilder, MediaTypeVersion mediaTypeVersion)
    {
        return webHostBuilder.AddKubernetesActuators(null, mediaTypeVersion);
    }

    /// <summary>
    /// Adds all standard and Kubernetes-specific actuators to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <param name="configureEndpoints">
    /// Customize endpoint behavior. Useful for tailoring auth requirements.
    /// </param>
    /// <param name="mediaTypeVersion">
    /// Specify the media type version to use in the response.
    /// </param>
    public static IHostBuilder AddKubernetesActuators(this IHostBuilder hostBuilder, Action<IEndpointConventionBuilder> configureEndpoints = null,
        MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
    {
        return hostBuilder.ConfigureLogging((_, configureLogging) => configureLogging.AddDynamicConsole()).ConfigureServices((context, collection) =>
        {
            collection.AddKubernetesActuators(context.Configuration, version: mediaTypeVersion);
            collection.ActivateActuatorEndpoints(configureEndpoints);
        });
    }

    /// <summary>
    /// Adds all standard and Kubernetes-specific actuators to the application.
    /// </summary>
    /// <param name="webHostBuilder">
    /// Your WebHostBuilder.
    /// </param>
    /// <param name="configureEndpoints">
    /// Customize endpoint behavior. Useful for tailoring auth requirements.
    /// </param>
    /// <param name="mediaTypeVersion">
    /// Specify the media type version to use in the response.
    /// </param>
    public static IWebHostBuilder AddKubernetesActuators(this IWebHostBuilder webHostBuilder, Action<IEndpointConventionBuilder> configureEndpoints = null,
        MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
    {
        return webHostBuilder.ConfigureLogging((_, configureLogging) => configureLogging.AddDynamicConsole()).ConfigureServices((context, collection) =>
        {
            collection.AddKubernetesActuators(context.Configuration, version: mediaTypeVersion);
            collection.ActivateActuatorEndpoints(configureEndpoints);
        });
    }

    /// <summary>
    /// Adds all standard and Kubernetes-specific actuators to the application.
    /// </summary>
    /// <param name="webApplicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    /// <param name="mediaTypeVersion">
    /// Specify the media type version to use in the response.
    /// </param>
    public static WebApplicationBuilder AddKubernetesActuators(this WebApplicationBuilder webApplicationBuilder, MediaTypeVersion mediaTypeVersion)
    {
        return webApplicationBuilder.AddKubernetesActuators(null, mediaTypeVersion);
    }

    /// <summary>
    /// Adds all standard and Kubernetes-specific actuators to the application.
    /// </summary>
    /// <param name="webApplicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    /// <param name="configureEndpoints">
    /// Customize endpoint behavior. Useful for tailoring auth requirements.
    /// </param>
    /// <param name="mediaTypeVersion">
    /// Specify the media type version to use in the response.
    /// </param>
    public static WebApplicationBuilder AddKubernetesActuators(this WebApplicationBuilder webApplicationBuilder,
        Action<IEndpointConventionBuilder> configureEndpoints = null, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
    {
        webApplicationBuilder.Logging.AddDynamicConsole();

        webApplicationBuilder.Services.AddKubernetesActuators(webApplicationBuilder.Configuration, version: mediaTypeVersion)
            .ActivateActuatorEndpoints(configureEndpoints);

        return webApplicationBuilder;
    }
}
