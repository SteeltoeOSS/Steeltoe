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
    /// Adds a configuration source to the <see cref="ConfigurationBuilder" /> that reads JSON from the SPRING_BOOT_APPLICATION environment variable and
    /// expands the child keys found within. Any '.' delimiters in configuration keys are converted to ':', which is the separator used by .NET.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddSpringBootFromEnvironmentVariable(this IConfigurationBuilder builder)
    {
        return AddSpringBootFromEnvironmentVariable(builder, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds a configuration source to the <see cref="ConfigurationBuilder" /> that reads JSON from the SPRING_BOOT_APPLICATION environment variable and
    /// expands the child keys found within. Any '.' delimiters in configuration keys are converted to ':', which is the separator used by .NET.
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
    public static IConfigurationBuilder AddSpringBootFromEnvironmentVariable(this IConfigurationBuilder builder, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        if (!builder.EnumerateSources<SpringBootEnvironmentVariableSource>().Any())
        {
            builder.Add(new SpringBootEnvironmentVariableSource());
        }

        return builder;
    }

#pragma warning disable SA1629 // Documentation text should end with a period
    /// <summary>
    /// Adds a configuration source to the <see cref="ConfigurationBuilder" /> that reads from the command-line and expands the child keys found within. Any
    /// '.' delimiters in configuration keys are converted to ':', which is the separator used by .NET.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <param name="args">
    /// The command-line arguments. Use <code><![CDATA[
    /// System.Environment.GetCommandLineArgs().Skip(1).ToArray()
    /// ]]></code> if unavailable.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddSpringBootFromCommandLine(this IConfigurationBuilder builder, string[] args)
#pragma warning restore SA1629 // Documentation text should end with a period
    {
        return AddSpringBootFromCommandLine(builder, args, NullLoggerFactory.Instance);
    }

#pragma warning disable SA1629 // Documentation text should end with a period
    /// <summary>
    /// Adds a configuration source to the <see cref="ConfigurationBuilder" /> that reads from the command-line and expands the child keys found within. Any
    /// '.' delimiters in configuration keys are converted to ':', which is the separator used by .NET.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <param name="args">
    /// The command-line arguments. Use <code><![CDATA[
    /// System.Environment.GetCommandLineArgs().Skip(1).ToArray()
    /// ]]></code> if unavailable.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddSpringBootFromCommandLine(this IConfigurationBuilder builder, string[] args, ILoggerFactory loggerFactory)
#pragma warning restore SA1629 // Documentation text should end with a period
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        if (!builder.EnumerateSources<SpringBootCommandLineSource>().Any())
        {
            builder.Add(new SpringBootCommandLineSource(args));
        }

        return builder;
    }
}
