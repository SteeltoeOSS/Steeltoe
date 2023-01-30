// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;

namespace Steeltoe.Configuration.Encryption;

public static class EncryptionResolverExtensions
{
    /// <summary>
    /// Creates a new <see cref="IConfiguration" /> using a <see cref="EncryptionResolverProvider" /> which wraps the provided <see cref="IConfiguration" />
    /// . The new configuration will then be used to replace the current <see cref="IConfiguration" /> in the service container. All subsequent requests for
    /// a <see cref="IConfiguration" /> will return the newly created <see cref="IConfiguration" /> providing encryption resolution.
    /// </summary>
    /// <param name="services">
    /// The service container.
    /// </param>
    /// <param name="configuration">
    /// The configuration the encryption resolver will wrap.
    /// </param>
    /// <param name="textDecryptor">
    /// The the decryptor to use
    /// </param>
    /// <returns>
    /// The new configuration.
    /// </returns>
    public static IConfiguration ConfigureEncryptionResolver(this IServiceCollection services, IConfiguration configuration, ITextDecryptor textDecryptor)
    {
        return ConfigureEncryptionResolver(services, configuration, NullLoggerFactory.Instance, textDecryptor);
    }

    /// <summary>
    /// Creates a new <see cref="IConfiguration" /> using a <see cref="EncryptionResolverProvider" /> which wraps the provided <see cref="IConfiguration" />
    /// . The new configuration will then be used to replace the current <see cref="IConfiguration" /> in the service container. All subsequent requests for
    /// a <see cref="IConfiguration" /> will return the newly created <see cref="IConfiguration" /> providing encryption resolution.
    /// </summary>
    /// <param name="services">
    /// The service container.
    /// </param>
    /// <param name="configuration">
    /// The configuration the encryption resolver will wrap.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <param name="textDecryptor">
    /// The the decryptor to use
    /// </param>
    /// <returns>
    /// The new configuration.
    /// </returns>
    public static IConfiguration ConfigureEncryptionResolver(this IServiceCollection services, IConfiguration configuration, ILoggerFactory loggerFactory, ITextDecryptor textDecryptor)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(loggerFactory);
        ArgumentGuard.NotNull(textDecryptor);

        IConfiguration newConfiguration = configuration.AddEncryptionResolver(loggerFactory, textDecryptor);
        services.Replace(ServiceDescriptor.Singleton(typeof(IConfiguration), newConfiguration));
        services.Replace(ServiceDescriptor.Singleton(typeof(ITextDecryptor), textDecryptor));

        return newConfiguration;
    }

    /// <summary>
    /// Adds a encryption resolver configuration source to the <see cref="ConfigurationBuilder" />. The encryption resolver source will capture and wrap
    /// all the existing sources <see cref="IConfigurationSource" /> contained in the builder.  The newly created source will then replace the existing
    /// sources and provide encryption resolution for the configuration. Typically, you will want to add this configuration source as the last one so that
    /// you wrap all of the applications' configuration sources with encryption resolution.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    /// <param name="textDecryptor">
    /// The the decryptor to use
    /// </param>
    /// <returns>
    /// The provided host builder.
    /// </returns>
    public static IWebHostBuilder AddEncryptionResolver(this IWebHostBuilder hostBuilder, ITextDecryptor textDecryptor)
    {
        return AddEncryptionResolver(hostBuilder, NullLoggerFactory.Instance, textDecryptor);
    }

    /// <summary>
    /// Adds a encryption resolver configuration source to the <see cref="ConfigurationBuilder" />. The encryption resolver source will capture and wrap
    /// all the existing sources <see cref="IConfigurationSource" /> contained in the builder.  The newly created source will then replace the existing
    /// sources and provide encryption resolution for the configuration. Typically, you will want to add this configuration source as the last one so that
    /// you wrap all of the applications' configuration sources with encryption resolution.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <param name="textDecryptor">
    /// The the decryptor to use
    /// </param>
    /// <returns>
    /// The provided host builder.
    /// </returns>
    public static IWebHostBuilder AddEncryptionResolver(this IWebHostBuilder hostBuilder, ILoggerFactory loggerFactory, ITextDecryptor textDecryptor)
    {
        ArgumentGuard.NotNull(hostBuilder);
        ArgumentGuard.NotNull(loggerFactory);
        ArgumentGuard.NotNull(textDecryptor);

        return hostBuilder.ConfigureAppConfiguration((_, builder) => builder.AddEncryptionResolver(loggerFactory, textDecryptor));
    }

    /// <summary>
    /// Adds a encryption resolver configuration source to the <see cref="ConfigurationBuilder" />. The encryption resolver source will capture and wrap
    /// all the existing sources <see cref="IConfigurationSource" /> contained in the builder.  The newly created source will then replace the existing
    /// sources and provide encryption resolution for the configuration. Typically, you will want to add this configuration source as the last one so that
    /// you wrap all of the applications configuration sources with encryption resolution.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    /// <param name="textDecryptor">
    /// The the decryptor to use
    /// </param>
    /// <returns>
    /// The provided host builder.
    /// </returns>
    public static IHostBuilder AddEncryptionResolver(this IHostBuilder hostBuilder, ITextDecryptor textDecryptor)
    {
        return AddEncryptionResolver(hostBuilder, NullLoggerFactory.Instance, textDecryptor);
    }

    /// <summary>
    /// Adds a encryption resolver configuration source to the <see cref="ConfigurationBuilder" />. The encryption resolver source will capture and wrap
    /// all the existing sources <see cref="IConfigurationSource" /> contained in the builder.  The newly created source will then replace the existing
    /// sources and provide encryption resolution for the configuration. Typically, you will want to add this configuration source as the last one so that
    /// you wrap all of the applications configuration sources with encryption resolution.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <param name="textDecryptor">
    /// The the decryptor to use
    /// </param>
    /// <returns>
    /// The provided host builder.
    /// </returns>
    public static IHostBuilder AddEncryptionResolver(this IHostBuilder hostBuilder, ILoggerFactory loggerFactory, ITextDecryptor textDecryptor)
    {
        ArgumentGuard.NotNull(hostBuilder);
        ArgumentGuard.NotNull(loggerFactory);
        ArgumentGuard.NotNull(textDecryptor);

        return hostBuilder.ConfigureAppConfiguration((_, builder) => builder.AddEncryptionResolver(loggerFactory, textDecryptor));
    }

    /// <summary>
    /// Adds a encryption resolver configuration source to the <see cref="ConfigurationBuilder" />. The encryption resolver source will capture and wrap
    /// all the existing sources <see cref="IConfigurationSource" /> contained in the builder.  The newly created source will then replace the existing
    /// sources and provide encryption resolution for the configuration. Typically, you will want to add this configuration source as the last one so that
    /// you wrap all of the applications configuration sources with encryption resolution.
    /// </summary>
    /// <param name="applicationBuilder">
    /// The application builder.
    /// </param>
    /// <param name="textDecryptor">
    /// The the decryptor to use
    /// </param>
    /// <returns>
    /// The provided application builder.
    /// </returns>
    public static WebApplicationBuilder AddEncryptionResolver(this WebApplicationBuilder applicationBuilder, ITextDecryptor textDecryptor)
    {
        return AddEncryptionResolver(applicationBuilder, NullLoggerFactory.Instance, textDecryptor);
    }

    /// <summary>
    /// Adds a encryption resolver configuration source to the <see cref="ConfigurationBuilder" />. The encryption resolver source will capture and wrap
    /// all the existing sources <see cref="IConfigurationSource" /> contained in the builder.  The newly created source will then replace the existing
    /// sources and provide encryption resolution for the configuration. Typically, you will want to add this configuration source as the last one so that
    /// you wrap all of the applications configuration sources with encryption resolution.
    /// </summary>
    /// <param name="applicationBuilder">
    /// The application builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <param name="textDecryptor">
    /// The the decryptor to use
    /// </param>
    /// <returns>
    /// The provided application builder.
    /// </returns>
    public static WebApplicationBuilder AddEncryptionResolver(this WebApplicationBuilder applicationBuilder, ILoggerFactory loggerFactory, ITextDecryptor textDecryptor)
    {
        ArgumentGuard.NotNull(applicationBuilder);
        ArgumentGuard.NotNull(loggerFactory);
        ArgumentGuard.NotNull(textDecryptor);

        applicationBuilder.Configuration.AddEncryptionResolver(loggerFactory, textDecryptor);
        return applicationBuilder;
    }
}
