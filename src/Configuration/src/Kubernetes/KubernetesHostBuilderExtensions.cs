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
using Steeltoe.Common.Hosting;

namespace Steeltoe.Configuration.Kubernetes;

public static class KubernetesHostBuilderExtensions
{
    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="builder">
    /// The host builder.
    /// </param>
    public static IWebHostBuilder AddKubernetesConfiguration(this IWebHostBuilder builder)
    {
        return AddKubernetesConfiguration(builder, null, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="builder">
    /// The host builder.
    /// </param>
    /// <param name="configureClient">
    /// Enables customization of the <see cref="KubernetesClientConfiguration" />.
    /// </param>
    public static IWebHostBuilder AddKubernetesConfiguration(this IWebHostBuilder builder, Action<KubernetesClientConfiguration> configureClient)
    {
        return AddKubernetesConfiguration(builder, configureClient, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="builder">
    /// The host builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public static IWebHostBuilder AddKubernetesConfiguration(this IWebHostBuilder builder, ILoggerFactory loggerFactory)
    {
        return AddKubernetesConfiguration(builder, null, loggerFactory);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="builder">
    /// The host builder.
    /// </param>
    /// <param name="configureClient">
    /// Enables customization of the <see cref="KubernetesClientConfiguration" />.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public static IWebHostBuilder AddKubernetesConfiguration(this IWebHostBuilder builder, Action<KubernetesClientConfiguration>? configureClient,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(loggerFactory);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddKubernetesConfiguration(configureClient, loggerFactory);

        return builder;
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="builder">
    /// The host builder.
    /// </param>
    public static IHostBuilder AddKubernetesConfiguration(this IHostBuilder builder)
    {
        return AddKubernetesConfiguration(builder, null, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="builder">
    /// The host builder.
    /// </param>
    /// <param name="configureClient">
    /// Enables customization of the <see cref="KubernetesClientConfiguration" />.
    /// </param>
    public static IHostBuilder AddKubernetesConfiguration(this IHostBuilder builder, Action<KubernetesClientConfiguration> configureClient)
    {
        return AddKubernetesConfiguration(builder, configureClient, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="builder">
    /// The host builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public static IHostBuilder AddKubernetesConfiguration(this IHostBuilder builder, ILoggerFactory loggerFactory)
    {
        return AddKubernetesConfiguration(builder, null, loggerFactory);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="builder">
    /// The host builder.
    /// </param>
    /// <param name="configureClient">
    /// Enables customization of the <see cref="KubernetesClientConfiguration" />.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public static IHostBuilder AddKubernetesConfiguration(this IHostBuilder builder, Action<KubernetesClientConfiguration>? configureClient,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(loggerFactory);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddKubernetesConfiguration(configureClient, loggerFactory);

        return builder;
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="builder">
    /// The web application builder.
    /// </param>
    public static WebApplicationBuilder AddKubernetesConfiguration(this WebApplicationBuilder builder)
    {
        return AddKubernetesConfiguration(builder, null, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="builder">
    /// The web application builder.
    /// </param>
    /// <param name="configureClient">
    /// Enables customization of the <see cref="KubernetesClientConfiguration" />.
    /// </param>
    public static WebApplicationBuilder AddKubernetesConfiguration(this WebApplicationBuilder builder, Action<KubernetesClientConfiguration> configureClient)
    {
        return AddKubernetesConfiguration(builder, configureClient, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="builder">
    /// The web application builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public static WebApplicationBuilder AddKubernetesConfiguration(this WebApplicationBuilder builder, ILoggerFactory loggerFactory)
    {
        return AddKubernetesConfiguration(builder, null, loggerFactory);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="builder">
    /// The web application builder.
    /// </param>
    /// <param name="configureClient">
    /// Enables customization of the <see cref="KubernetesClientConfiguration" />.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public static WebApplicationBuilder AddKubernetesConfiguration(this WebApplicationBuilder builder, Action<KubernetesClientConfiguration>? configureClient,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(loggerFactory);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddKubernetesConfiguration(configureClient, loggerFactory);

        return builder;
    }
}
