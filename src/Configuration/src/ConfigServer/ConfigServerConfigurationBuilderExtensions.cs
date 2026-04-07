// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Configuration.Kubernetes.ServiceBindings;

namespace Steeltoe.Configuration.ConfigServer;

/// <summary>
/// Extension methods for adding <see cref="ConfigServerConfigurationProvider" />.
/// </summary>
public static class ConfigServerConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds a configuration source for Config Server to the <see cref="ConfigurationBuilder" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder builder)
    {
        return AddConfigServer(builder, new ConfigServerClientOptions(), null, null, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds a configuration source for Config Server to the <see cref="ConfigurationBuilder" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder builder, ILoggerFactory loggerFactory)
    {
        return AddConfigServer(builder, new ConfigServerClientOptions(), null, null, loggerFactory);
    }

    /// <summary>
    /// Adds a configuration source for Config Server to the <see cref="ConfigurationBuilder" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <param name="options">
    /// The initial options, whose values are overridden from <see cref="IConfiguration" />.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder builder, ConfigServerClientOptions options)
    {
        return AddConfigServer(builder, options, null, null, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds a configuration source for Config Server to the <see cref="ConfigurationBuilder" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <param name="options">
    /// The initial options, whose values are overridden from <see cref="IConfiguration" />.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder builder, ConfigServerClientOptions options, ILoggerFactory loggerFactory)
    {
        return AddConfigServer(builder, options, null, null, loggerFactory);
    }

    /// <summary>
    /// Adds a configuration source for Config Server to the <see cref="ConfigurationBuilder" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <param name="configure">
    /// An optional delegate that further configures options from code, after settings from <see cref="IConfiguration" /> have been applied.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder builder, Action<ConfigServerClientOptions>? configure)
    {
        return AddConfigServer(builder, new ConfigServerClientOptions(), configure, null, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds a configuration source for Config Server to the <see cref="ConfigurationBuilder" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <param name="configure">
    /// An optional delegate that further configures options from code, after settings from <see cref="IConfiguration" /> have been applied.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder builder, Action<ConfigServerClientOptions>? configure,
        ILoggerFactory loggerFactory)
    {
        return AddConfigServer(builder, new ConfigServerClientOptions(), configure, null, loggerFactory);
    }

    internal static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder builder, ConfigServerClientOptions options,
        Action<ConfigServerClientOptions>? configure, Func<HttpClientHandler>? createHttpClientHandler, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        if (!builder.EnumerateSources<ConfigServerConfigurationSource>().Any())
        {
            builder.AddCloudFoundry();
            builder.AddKubernetesServiceBindings();

            var source = new ConfigServerConfigurationSource(options, builder.Sources, builder.Properties, configure, createHttpClientHandler, loggerFactory);
            builder.Add(source);
        }

        return builder;
    }
}
