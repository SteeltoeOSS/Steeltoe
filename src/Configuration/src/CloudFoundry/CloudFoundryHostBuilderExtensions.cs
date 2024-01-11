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

namespace Steeltoe.Configuration.CloudFoundry;

public static class CloudFoundryHostBuilderExtensions
{
    /// <summary>
    /// Adds the Cloud Foundry configuration provider.
    /// </summary>
    /// <param name="builder">
    /// The host builder.
    /// </param>
    public static IWebHostBuilder AddCloudFoundryConfiguration(this IWebHostBuilder builder)
    {
        return AddCloudFoundryConfiguration(builder, BootstrapLoggerFactory.Default);
    }

    /// <summary>
    /// Adds the Cloud Foundry configuration provider.
    /// </summary>
    /// <param name="builder">
    /// The host builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging, or <see cref="BootstrapLoggerFactory.Default" /> to
    /// write only to the console until logging is fully initialized.
    /// </param>
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
    /// The host builder.
    /// </param>
    public static IHostBuilder AddCloudFoundryConfiguration(this IHostBuilder builder)
    {
        return AddCloudFoundryConfiguration(builder, BootstrapLoggerFactory.Default);
    }

    /// <summary>
    /// Adds the Cloud Foundry configuration provider.
    /// </summary>
    /// <param name="builder">
    /// The host builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging, or <see cref="BootstrapLoggerFactory.Default" /> to
    /// write only to the console until logging is fully initialized.
    /// </param>
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
    /// The application builder.
    /// </param>
    public static WebApplicationBuilder AddCloudFoundryConfiguration(this WebApplicationBuilder builder)
    {
        return AddCloudFoundryConfiguration(builder, BootstrapLoggerFactory.Default);
    }

    /// <summary>
    /// Adds the Cloud Foundry configuration provider.
    /// </summary>
    /// <param name="builder">
    /// The application builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging, or <see cref="BootstrapLoggerFactory.Default" /> to
    /// write only to the console until logging is fully initialized.
    /// </param>
    public static WebApplicationBuilder AddCloudFoundryConfiguration(this WebApplicationBuilder builder, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddCloudFoundryConfiguration(loggerFactory);

        return builder;
    }
}
