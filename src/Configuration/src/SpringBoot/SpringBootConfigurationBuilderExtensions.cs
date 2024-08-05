// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Steeltoe.Configuration.SpringBoot;

public static class SpringBootConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds a configuration source to the <see cref="ConfigurationBuilder" /> that reads from the SPRING_BOOT_APPLICATION environment variable and expands
    /// the child keys found within. Configuration keys in '.' delimited style are also converted to a format understood by .NET.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder.
    /// </param>
    /// <returns>
    /// <paramref name="builder" />.
    /// </returns>
    public static IConfigurationBuilder AddSpringBootFromEnvironmentVariable(this IConfigurationBuilder builder)
    {
        return AddSpringBootFromEnvironmentVariable(builder, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds a configuration source to the <see cref="ConfigurationBuilder" /> that reads from the SPRING_BOOT_APPLICATION environment variable and expands
    /// the child keys found within. Configuration keys in '.' delimited style are also converted to a format understood by .NET.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// <paramref name="builder" />.
    /// </returns>
    public static IConfigurationBuilder AddSpringBootFromEnvironmentVariable(this IConfigurationBuilder builder, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        builder.Add(new SpringBootEnvironmentVariableSource());

        return builder;
    }

    /// <summary>
    /// Adds a configuration source to the <see cref="ConfigurationBuilder" /> that reads from the command-line and expands the child keys found within.
    /// Configuration keys in '.' delimited style are also converted to a format understood by .NET.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder.
    /// </param>
    /// <param name="configuration">
    /// The configuration.
    /// </param>
    /// <returns>
    /// <paramref name="builder" />.
    /// </returns>
    public static IConfigurationBuilder AddSpringBootFromCommandLine(this IConfigurationBuilder builder, IConfiguration configuration)
    {
        return AddSpringBootFromCommandLine(builder, configuration, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds a configuration source to the <see cref="ConfigurationBuilder" /> that reads from the command-line and expands the child keys found within.
    /// Configuration keys in '.' delimited style are also converted to a format understood by .NET.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder.
    /// </param>
    /// <param name="configuration">
    /// The configuration.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// <paramref name="builder" />.
    /// </returns>
    public static IConfigurationBuilder AddSpringBootFromCommandLine(this IConfigurationBuilder builder, IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        builder.Add(new SpringBootCommandLineSource(configuration));

        return builder;
    }
}
