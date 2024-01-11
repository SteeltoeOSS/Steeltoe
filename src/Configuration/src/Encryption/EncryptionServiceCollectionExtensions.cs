// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;
using Steeltoe.Configuration.Encryption.Decryption;

namespace Steeltoe.Configuration.Encryption;

public static class EncryptionServiceCollectionExtensions
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
    /// <returns>
    /// The new configuration.
    /// </returns>
    public static IConfiguration ConfigureEncryptionResolver(this IServiceCollection services, IConfiguration configuration)
    {
        ITextDecryptor textDecryptor = ConfigServerEncryptionSettings.CreateTextDecryptor(configuration);
        return ConfigureEncryptionResolver(services, configuration, textDecryptor, NullLoggerFactory.Instance);
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
    /// <param name="textDecryptor">
    /// The decryptor to use.
    /// </param>
    /// <returns>
    /// The new configuration.
    /// </returns>
    public static IConfiguration ConfigureEncryptionResolver(this IServiceCollection services, IConfiguration configuration, ITextDecryptor textDecryptor)
    {
        return ConfigureEncryptionResolver(services, configuration, textDecryptor, NullLoggerFactory.Instance);
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
    /// <param name="textDecryptor">
    /// The decryptor to use.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The new configuration.
    /// </returns>
    public static IConfiguration ConfigureEncryptionResolver(this IServiceCollection services, IConfiguration configuration, ITextDecryptor textDecryptor,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(loggerFactory);
        ArgumentGuard.NotNull(textDecryptor);

        IConfiguration newConfiguration = configuration.AddEncryptionResolver(textDecryptor, loggerFactory);
        services.Replace(ServiceDescriptor.Singleton(typeof(IConfiguration), newConfiguration));
        services.Replace(ServiceDescriptor.Singleton(typeof(ITextDecryptor), textDecryptor));

        return newConfiguration;
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
    /// <returns>
    /// The new configuration.
    /// </returns>
    public static IConfiguration ConfigureConfigServerEncryptionResolver(this IServiceCollection services, IConfiguration configuration)
    {
        return ConfigureConfigServerEncryptionResolver(services, configuration, NullLoggerFactory.Instance);
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
    /// <returns>
    /// The new configuration.
    /// </returns>
    public static IConfiguration ConfigureConfigServerEncryptionResolver(this IServiceCollection services, IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(loggerFactory);

        ITextDecryptor textDecryptor = ConfigServerEncryptionSettings.CreateTextDecryptor(configuration);

        IConfiguration newConfiguration = configuration.AddEncryptionResolver(textDecryptor, loggerFactory);
        services.Replace(ServiceDescriptor.Singleton(typeof(IConfiguration), newConfiguration));

        return newConfiguration;
    }
}
