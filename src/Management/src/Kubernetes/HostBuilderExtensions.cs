// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
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
    public static IHostBuilder AddKubernetesActuators(this IHostBuilder hostBuilder)
    {
        return hostBuilder.AddKubernetesActuators(null);
    }

    /// <summary>
    /// Adds all standard and Kubernetes-specific actuators to the application.
    /// </summary>
    /// <param name="webHostBuilder">
    /// Your WebHostBuilder.
    /// </param>
    public static IWebHostBuilder AddKubernetesActuators(this IWebHostBuilder webHostBuilder)
    {
        return webHostBuilder.AddKubernetesActuators(null);
    }

    /// <summary>
    /// Adds all standard and Kubernetes-specific actuators to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <param name="configureEndpoints">
    /// Customize endpointHandler behavior. Useful for tailoring auth requirements.
    /// </param>
    public static IHostBuilder AddKubernetesActuators(this IHostBuilder hostBuilder, Action<IEndpointConventionBuilder> configureEndpoints)
    {
        return hostBuilder.ConfigureLogging((_, configureLogging) => configureLogging.AddDynamicConsole()).ConfigureServices((context, collection) =>
        {
            collection.AddKubernetesActuators();
            IEndpointConventionBuilder epBuilder = collection.ActivateActuatorEndpoints();
            configureEndpoints?.Invoke(epBuilder);
        });
    }

    /// <summary>
    /// Adds all standard and Kubernetes-specific actuators to the application.
    /// </summary>
    /// <param name="webHostBuilder">
    /// Your WebHostBuilder.
    /// </param>
    /// <param name="configureEndpoints">
    /// Customize endpointHandler behavior. Useful for tailoring auth requirements.
    /// </param>
    public static IWebHostBuilder AddKubernetesActuators(this IWebHostBuilder webHostBuilder, Action<IEndpointConventionBuilder> configureEndpoints)
    {
        return webHostBuilder.ConfigureLogging((_, configureLogging) => configureLogging.AddDynamicConsole()).ConfigureServices((context, collection) =>
        {
            collection.AddKubernetesActuators();
            IEndpointConventionBuilder epBuilder = collection.ActivateActuatorEndpoints();
            configureEndpoints?.Invoke(epBuilder);
        });
    }

    /// <summary>
    /// Adds all standard and Kubernetes-specific actuators to the application.
    /// </summary>
    /// <param name="webApplicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddKubernetesActuators(this WebApplicationBuilder webApplicationBuilder)
    {
        return webApplicationBuilder.AddKubernetesActuators(null);
    }

    /// <summary>
    /// Adds all standard and Kubernetes-specific actuators to the application.
    /// </summary>
    /// <param name="webApplicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    /// <param name="configureEndpoints">
    /// Customize endpointHandler behavior. Useful for tailoring auth requirements.
    /// </param>
    public static WebApplicationBuilder AddKubernetesActuators(this WebApplicationBuilder webApplicationBuilder,
        Action<IEndpointConventionBuilder> configureEndpoints)
    {
        ArgumentGuard.NotNull(webApplicationBuilder);

        webApplicationBuilder.Logging.AddDynamicConsole();

        IServiceCollection services = webApplicationBuilder.Services.AddKubernetesActuators();
        IEndpointConventionBuilder epBuilder = services.ActivateActuatorEndpoints();
        configureEndpoints?.Invoke(epBuilder);

        return webApplicationBuilder;
    }
}
