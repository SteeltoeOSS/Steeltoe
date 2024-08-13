// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Hosting;
using Steeltoe.Common.Logging;

namespace Steeltoe.Configuration.Encryption;

public static class EncryptionHostBuilderExtensions
{
    /// <summary>
    /// Adds an encryption resolver configuration source to the <see cref="ConfigurationBuilder" />. The encryption resolver source will capture and wrap all
    /// the existing sources <see cref="IConfigurationSource" /> contained in the builder.  The newly created source will then replace the existing sources
    /// and provide encryption resolution for the configuration. Typically, you will want to add this configuration source as the last one so that you wrap
    /// all applications' configuration sources with encryption resolution.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddEncryptionResolver(this IWebHostBuilder builder)
    {
        return AddEncryptionResolver(builder, BootstrapLoggerFactory.Default);
    }

    /// <summary>
    /// Adds an encryption resolver configuration source to the <see cref="ConfigurationBuilder" />. The encryption resolver source will capture and wrap all
    /// the existing sources <see cref="IConfigurationSource" /> contained in the builder.  The newly created source will then replace the existing sources
    /// and provide encryption resolution for the configuration. Typically, you will want to add this configuration source as the last one so that you wrap
    /// all applications' configuration sources with encryption resolution.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging, or <see cref="BootstrapLoggerFactory.Default" /> to
    /// write only to the console until logging is fully initialized.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddEncryptionResolver(this IWebHostBuilder builder, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddEncryptionResolver(loggerFactory);

        return builder;
    }

    /// <summary>
    /// Adds an encryption resolver configuration source to the <see cref="ConfigurationBuilder" />. The encryption resolver source will capture and wrap all
    /// the existing sources <see cref="IConfigurationSource" /> contained in the builder.  The newly created source will then replace the existing sources
    /// and provide encryption resolution for the configuration. Typically, you will want to add this configuration source as the last one so that you wrap
    /// all applications' configuration sources with encryption resolution.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostBuilder AddEncryptionResolver(this IHostBuilder builder)
    {
        return AddEncryptionResolver(builder, BootstrapLoggerFactory.Default);
    }

    /// <summary>
    /// Adds an encryption resolver configuration source to the <see cref="ConfigurationBuilder" />. The encryption resolver source will capture and wrap all
    /// the existing sources <see cref="IConfigurationSource" /> contained in the builder.  The newly created source will then replace the existing sources
    /// and provide encryption resolution for the configuration. Typically, you will want to add this configuration source as the last one so that you wrap
    /// all applications' configuration sources with encryption resolution.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostBuilder" /> to configure.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging, or <see cref="BootstrapLoggerFactory.Default" /> to
    /// write only to the console until logging is fully initialized.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostBuilder AddEncryptionResolver(this IHostBuilder builder, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddEncryptionResolver(loggerFactory);

        return builder;
    }

    /// <summary>
    /// Adds an encryption resolver configuration source to the <see cref="ConfigurationBuilder" />. The encryption resolver source will capture and wrap all
    /// the existing sources <see cref="IConfigurationSource" /> contained in the builder.  The newly created source will then replace the existing sources
    /// and provide encryption resolution for the configuration. Typically, you will want to add this configuration source as the last one so that you wrap
    /// all applications' configuration sources with encryption resolution.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddEncryptionResolver(this IHostApplicationBuilder builder)
    {
        return AddEncryptionResolver(builder, BootstrapLoggerFactory.Default);
    }

    /// <summary>
    /// Adds an encryption resolver configuration source to the <see cref="ConfigurationBuilder" />. The encryption resolver source will capture and wrap all
    /// the existing sources <see cref="IConfigurationSource" /> contained in the builder.  The newly created source will then replace the existing sources
    /// and provide encryption resolution for the configuration. Typically, you will want to add this configuration source as the last one so that you wrap
    /// all applications' configuration sources with encryption resolution.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging, or <see cref="BootstrapLoggerFactory.Default" /> to
    /// write only to the console until logging is fully initialized.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddEncryptionResolver(this IHostApplicationBuilder builder, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddEncryptionResolver(loggerFactory);

        return builder;
    }
}
