// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Common.Hosting;
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
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <param name="configureEndpoints">
    /// Customize endpoint behavior. Useful for tailoring auth requirements.
    /// </param>
    public static IHostBuilder AddKubernetesActuators(this IHostBuilder hostBuilder, Action<IEndpointConventionBuilder>? configureEndpoints)
    {
        ArgumentGuard.NotNull(hostBuilder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(hostBuilder);
        wrapper.AddKubernetesActuators(configureEndpoints);

        return hostBuilder;
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
    /// <param name="webHostBuilder">
    /// Your WebHostBuilder.
    /// </param>
    /// <param name="configureEndpoints">
    /// Customize endpoint behavior. Useful for tailoring auth requirements.
    /// </param>
    public static IWebHostBuilder AddKubernetesActuators(this IWebHostBuilder webHostBuilder, Action<IEndpointConventionBuilder>? configureEndpoints)
    {
        ArgumentGuard.NotNull(webHostBuilder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(webHostBuilder);
        wrapper.AddKubernetesActuators(configureEndpoints);

        return webHostBuilder;
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
    /// Customize endpoint behavior. Useful for tailoring auth requirements.
    /// </param>
    public static WebApplicationBuilder AddKubernetesActuators(this WebApplicationBuilder webApplicationBuilder,
        Action<IEndpointConventionBuilder>? configureEndpoints)
    {
        ArgumentGuard.NotNull(webApplicationBuilder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(webApplicationBuilder);
        wrapper.AddKubernetesActuators(configureEndpoints);

        return webApplicationBuilder;
    }

    internal static void AddKubernetesActuators(this HostBuilderWrapper wrapper, Action<IEndpointConventionBuilder>? configureEndpoints)
    {
        wrapper.ConfigureLogging(loggingBuilder => loggingBuilder.AddDynamicConsole());

        wrapper.ConfigureServices(services =>
        {
            services.AddKubernetesActuators();

            IEndpointConventionBuilder endpointBuilder = services.ActivateActuatorEndpoints();
            configureEndpoints?.Invoke(endpointBuilder);
        });
    }
}
