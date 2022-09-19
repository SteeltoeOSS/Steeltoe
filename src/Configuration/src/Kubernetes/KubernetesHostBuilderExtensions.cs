// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;

namespace Steeltoe.Extensions.Configuration.Kubernetes;

public static class KubernetesHostBuilderExtensions
{
    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    public static IWebHostBuilder AddKubernetesConfiguration(this IWebHostBuilder hostBuilder)
    {
        return AddKubernetesConfiguration(hostBuilder, null, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    /// <param name="configureClient">
    /// Enables customization of the <see cref="KubernetesClientConfiguration" />.
    /// </param>
    public static IWebHostBuilder AddKubernetesConfiguration(this IWebHostBuilder hostBuilder, Action<KubernetesClientConfiguration> configureClient)
    {
        return AddKubernetesConfiguration(hostBuilder, configureClient, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public static IWebHostBuilder AddKubernetesConfiguration(this IWebHostBuilder hostBuilder, ILoggerFactory loggerFactory)
    {
        return AddKubernetesConfiguration(hostBuilder, null, loggerFactory);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    /// <param name="configureClient">
    /// Enables customization of the <see cref="KubernetesClientConfiguration" />.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public static IWebHostBuilder AddKubernetesConfiguration(this IWebHostBuilder hostBuilder, Action<KubernetesClientConfiguration> configureClient,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(hostBuilder);
        ArgumentGuard.NotNull(loggerFactory);

        hostBuilder.ConfigureAppConfiguration(builder => builder.AddKubernetes(configureClient, loggerFactory));
        hostBuilder.ConfigureServices(services => services.AddKubernetesConfigurationServices());

        return hostBuilder;
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    public static IHostBuilder AddKubernetesConfiguration(this IHostBuilder hostBuilder)
    {
        return AddKubernetesConfiguration(hostBuilder, null, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    /// <param name="configureClient">
    /// Enables customization of the <see cref="KubernetesClientConfiguration" />.
    /// </param>
    public static IHostBuilder AddKubernetesConfiguration(this IHostBuilder hostBuilder, Action<KubernetesClientConfiguration> configureClient)
    {
        return AddKubernetesConfiguration(hostBuilder, configureClient, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public static IHostBuilder AddKubernetesConfiguration(this IHostBuilder hostBuilder, ILoggerFactory loggerFactory)
    {
        return AddKubernetesConfiguration(hostBuilder, null, loggerFactory);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    /// <param name="configureClient">
    /// Enables customization of the <see cref="KubernetesClientConfiguration" />.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public static IHostBuilder AddKubernetesConfiguration(this IHostBuilder hostBuilder, Action<KubernetesClientConfiguration> configureClient,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(hostBuilder);
        ArgumentGuard.NotNull(loggerFactory);

        hostBuilder.ConfigureAppConfiguration(builder => builder.AddKubernetes(configureClient, loggerFactory));
        hostBuilder.ConfigureServices(services => services.AddKubernetesConfigurationServices());

        return hostBuilder;
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="applicationBuilder">
    /// The web application builder.
    /// </param>
    public static WebApplicationBuilder AddKubernetesConfiguration(this WebApplicationBuilder applicationBuilder)
    {
        return AddKubernetesConfiguration(applicationBuilder, null, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="applicationBuilder">
    /// The web application builder.
    /// </param>
    /// <param name="configureClient">
    /// Enables customization of the <see cref="KubernetesClientConfiguration" />.
    /// </param>
    public static WebApplicationBuilder AddKubernetesConfiguration(this WebApplicationBuilder applicationBuilder,
        Action<KubernetesClientConfiguration> configureClient)
    {
        return AddKubernetesConfiguration(applicationBuilder, configureClient, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="applicationBuilder">
    /// The web application builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public static WebApplicationBuilder AddKubernetesConfiguration(this WebApplicationBuilder applicationBuilder, ILoggerFactory loggerFactory)
    {
        return AddKubernetesConfiguration(applicationBuilder, null, loggerFactory);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="applicationBuilder">
    /// The web application builder.
    /// </param>
    /// <param name="configureClient">
    /// Enables customization of the <see cref="KubernetesClientConfiguration" />.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public static WebApplicationBuilder AddKubernetesConfiguration(this WebApplicationBuilder applicationBuilder,
        Action<KubernetesClientConfiguration> configureClient, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(applicationBuilder);
        ArgumentGuard.NotNull(loggerFactory);

        applicationBuilder.Configuration.AddKubernetes(configureClient, loggerFactory);
        applicationBuilder.Services.AddKubernetesConfigurationServices();

        return applicationBuilder;
    }
}
