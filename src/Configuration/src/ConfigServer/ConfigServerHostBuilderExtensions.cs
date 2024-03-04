// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;
using Steeltoe.Common.Hosting;
using Steeltoe.Common.Logging;

namespace Steeltoe.Configuration.ConfigServer;

public static class ConfigServerHostBuilderExtensions
{
    /// <summary>
    /// Adds Config Server and Cloud Foundry as application configuration sources. Adds Config Server health check contributor to the service container.
    /// </summary>
    /// <param name="builder">
    /// The host builder.
    /// </param>
    /// <returns>
    /// <see cref="IWebHostBuilder" /> with Config Server and Cloud Foundry Config Provider attached.
    /// </returns>
    public static IWebHostBuilder AddConfigServer(this IWebHostBuilder builder)
    {
        return AddConfigServer(builder, BootstrapLoggerFactory.Default);
    }

    /// <summary>
    /// Adds Config Server and Cloud Foundry as application configuration sources. Adds Config Server health check contributor to the service container.
    /// </summary>
    /// <param name="builder">
    /// The host builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging, or <see cref="BootstrapLoggerFactory.Default" /> to
    /// write only to the console until logging is fully initialized.
    /// </param>
    /// <returns>
    /// <see cref="IWebHostBuilder" /> with Config Server and Cloud Foundry Config Provider attached.
    /// </returns>
    public static IWebHostBuilder AddConfigServer(this IWebHostBuilder builder, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(loggerFactory);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddConfigServer(loggerFactory);

        return builder;
    }

    /// <summary>
    /// Adds Config Server and Cloud Foundry as application configuration sources. Adds Config Server health check contributor to the service container.
    /// </summary>
    /// <param name="builder">
    /// The host builder.
    /// </param>
    /// <returns>
    /// <see cref="IHostBuilder" /> with config server and Cloud Foundry Config Provider attached.
    /// </returns>
    public static IHostBuilder AddConfigServer(this IHostBuilder builder)
    {
        return AddConfigServer(builder, BootstrapLoggerFactory.Default);
    }

    /// <summary>
    /// Adds Config Server and Cloud Foundry as application configuration sources. Adds Config Server health check contributor to the service container.
    /// </summary>
    /// <param name="builder">
    /// The host builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging, or <see cref="BootstrapLoggerFactory.Default" /> to
    /// write only to the console until logging is fully initialized.
    /// </param>
    /// <returns>
    /// <see cref="IHostBuilder" /> with config server and Cloud Foundry Config Provider attached.
    /// </returns>
    public static IHostBuilder AddConfigServer(this IHostBuilder builder, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(loggerFactory);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddConfigServer(loggerFactory);

        return builder;
    }

    /// <summary>
    /// Adds Config Server and Cloud Foundry as application configuration sources. Also adds Config Server health check contributor and related services to
    /// the service container.
    /// </summary>
    /// <param name="builder">
    /// The application builder.
    /// </param>
    public static WebApplicationBuilder AddConfigServer(this WebApplicationBuilder builder)
    {
        return AddConfigServer(builder, BootstrapLoggerFactory.Default);
    }

    /// <summary>
    /// Adds Config Server and Cloud Foundry as application configuration sources. Also adds Config Server health check contributor and related services to
    /// the service container.
    /// </summary>
    /// <param name="builder">
    /// The application builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging, or <see cref="BootstrapLoggerFactory.Default" /> to
    /// write only to the console until logging is fully initialized.
    /// </param>
    public static WebApplicationBuilder AddConfigServer(this WebApplicationBuilder builder, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(loggerFactory);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddConfigServer(loggerFactory);

        return builder;
    }
}
