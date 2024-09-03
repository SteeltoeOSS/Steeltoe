// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Hosting;

namespace Steeltoe.Configuration.ConfigServer;

public static class ConfigServerHostBuilderExtensions
{
    /// <summary>
    /// Adds Config Server and Cloud Foundry as application configuration sources. Adds Config Server health check contributor to the service container.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddConfigServer(this IWebHostBuilder builder)
    {
        return AddConfigServer(builder, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds Config Server and Cloud Foundry as application configuration sources. Adds Config Server health check contributor to the service container.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddConfigServer(this IWebHostBuilder builder, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddConfigServer(loggerFactory);

        return builder;
    }

    /// <summary>
    /// Adds Config Server and Cloud Foundry as application configuration sources. Adds Config Server health check contributor to the service container.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostBuilder AddConfigServer(this IHostBuilder builder)
    {
        return AddConfigServer(builder, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds Config Server and Cloud Foundry as application configuration sources. Adds Config Server health check contributor to the service container.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostBuilder" /> to configure.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostBuilder AddConfigServer(this IHostBuilder builder, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddConfigServer(loggerFactory);

        return builder;
    }

    /// <summary>
    /// Adds Config Server and Cloud Foundry as application configuration sources. Also adds Config Server health check contributor and related services to
    /// the service container.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddConfigServer(this IHostApplicationBuilder builder)
    {
        return AddConfigServer(builder, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds Config Server and Cloud Foundry as application configuration sources. Also adds Config Server health check contributor and related services to
    /// the service container.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddConfigServer(this IHostApplicationBuilder builder, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddConfigServer(loggerFactory);

        return builder;
    }
}
