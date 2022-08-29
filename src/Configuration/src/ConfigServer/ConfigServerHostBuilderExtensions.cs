// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;

namespace Steeltoe.Extensions.Configuration.ConfigServer;

public static class ConfigServerHostBuilderExtensions
{
    /// <summary>
    /// Adds Config Server and Cloud Foundry as application configuration sources. Adds Config Server health check contributor to the service container.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    /// <returns>
    /// <see cref="IWebHostBuilder" /> with Config Server and Cloud Foundry Config Provider attached.
    /// </returns>
    public static IWebHostBuilder AddConfigServer(this IWebHostBuilder hostBuilder)
    {
        return AddConfigServer(hostBuilder, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds Config Server and Cloud Foundry as application configuration sources. Adds Config Server health check contributor to the service container.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// <see cref="IWebHostBuilder" /> with Config Server and Cloud Foundry Config Provider attached.
    /// </returns>
    public static IWebHostBuilder AddConfigServer(this IWebHostBuilder hostBuilder, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(hostBuilder);
        ArgumentGuard.NotNull(loggerFactory);

        hostBuilder.ConfigureAppConfiguration((context, builder) => builder.AddConfigServer(context.HostingEnvironment, loggerFactory));
        hostBuilder.ConfigureServices((_, services) => services.AddConfigServerServices());

        return hostBuilder;
    }

    /// <summary>
    /// Adds Config Server and Cloud Foundry as application configuration sources. Adds Config Server health check contributor to the service container.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    /// <returns>
    /// <see cref="IHostBuilder" /> with config server and Cloud Foundry Config Provider attached.
    /// </returns>
    public static IHostBuilder AddConfigServer(this IHostBuilder hostBuilder)
    {
        return AddConfigServer(hostBuilder, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds Config Server and Cloud Foundry as application configuration sources. Adds Config Server health check contributor to the service container.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// <see cref="IHostBuilder" /> with config server and Cloud Foundry Config Provider attached.
    /// </returns>
    public static IHostBuilder AddConfigServer(this IHostBuilder hostBuilder, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(hostBuilder);
        ArgumentGuard.NotNull(loggerFactory);

        hostBuilder.ConfigureAppConfiguration((context, builder) => builder.AddConfigServer(context.HostingEnvironment, loggerFactory));
        hostBuilder.ConfigureServices((_, services) => services.AddConfigServerServices());

        return hostBuilder;
    }

    /// <summary>
    /// Adds Config Server and Cloud Foundry as application configuration sources. Also adds Config Server health check contributor and related services to
    /// the service container.
    /// </summary>
    /// <param name="applicationBuilder">
    /// The application builder.
    /// </param>
    public static WebApplicationBuilder AddConfigServer(this WebApplicationBuilder applicationBuilder)
    {
        return AddConfigServer(applicationBuilder, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds Config Server and Cloud Foundry as application configuration sources. Also adds Config Server health check contributor and related services to
    /// the service container.
    /// </summary>
    /// <param name="applicationBuilder">
    /// The application builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public static WebApplicationBuilder AddConfigServer(this WebApplicationBuilder applicationBuilder, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(applicationBuilder);
        ArgumentGuard.NotNull(loggerFactory);

        applicationBuilder.Configuration.AddConfigServer(applicationBuilder.Environment, loggerFactory);
        applicationBuilder.Services.AddConfigServerServices();

        return applicationBuilder;
    }
}
