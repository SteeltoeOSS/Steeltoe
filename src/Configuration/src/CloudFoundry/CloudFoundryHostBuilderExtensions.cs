// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;
using Steeltoe.Common.Hosting;
using Steeltoe.Common.Logging;

namespace Steeltoe.Configuration.CloudFoundry;

public static class CloudFoundryHostBuilderExtensions
{
    /// <summary>
    /// Adds the Cloud Foundry configuration provider.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddCloudFoundryConfiguration(this IWebHostBuilder builder)
    {
        return AddCloudFoundryConfiguration(builder, BootstrapLoggerFactory.Default);
    }

    /// <summary>
    /// Adds the Cloud Foundry configuration provider.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging, or <see cref="BootstrapLoggerFactory.Default" /> to
    /// write only to the console until logging is fully initialized.
    /// </param>
    /// <returns>
    /// The <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddCloudFoundryConfiguration(this IWebHostBuilder builder, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddCloudFoundryConfiguration(loggerFactory);

        return builder;
    }

    /// <summary>
    /// Adds the Cloud Foundry configuration provider.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The <see cref="IHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IHostBuilder AddCloudFoundryConfiguration(this IHostBuilder builder)
    {
        return AddCloudFoundryConfiguration(builder, BootstrapLoggerFactory.Default);
    }

    /// <summary>
    /// Adds the Cloud Foundry configuration provider.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostBuilder" /> to configure.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging, or <see cref="BootstrapLoggerFactory.Default" /> to
    /// write only to the console until logging is fully initialized.
    /// </param>
    /// <returns>
    /// The <see cref="IHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IHostBuilder AddCloudFoundryConfiguration(this IHostBuilder builder, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddCloudFoundryConfiguration(loggerFactory);

        return builder;
    }

    /// <summary>
    /// Adds the Cloud Foundry configuration provider.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The <see cref="IHostApplicationBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddCloudFoundryConfiguration(this IHostApplicationBuilder builder)
    {
        return AddCloudFoundryConfiguration(builder, BootstrapLoggerFactory.Default);
    }

    /// <summary>
    /// Adds the Cloud Foundry configuration provider.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging, or <see cref="BootstrapLoggerFactory.Default" /> to
    /// write only to the console until logging is fully initialized.
    /// </param>
    /// <returns>
    /// The <see cref="IHostApplicationBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddCloudFoundryConfiguration(this IHostApplicationBuilder builder, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddCloudFoundryConfiguration(loggerFactory);

        return builder;
    }
}
