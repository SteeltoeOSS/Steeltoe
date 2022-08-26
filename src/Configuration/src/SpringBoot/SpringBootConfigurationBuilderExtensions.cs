// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Extensions.Configuration.SpringBoot;

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
        ArgumentGuard.NotNull(builder);

        builder.Add(new SpringBootEnvironmentVariableSource());

        return builder;
    }

    /// <summary>
    /// Adds a configuration source to the <see cref="ConfigurationBuilder" /> that reads from the command-line and expands the child keys found within.
    /// Configuration keys in '.' delimited style are also converted to a format understood by .NET.
    /// </summary>
    /// <param name="builder">
    /// /// The configuration builder.
    /// </param>
    /// <param name="configuration">
    /// /// The configuration.
    /// </param>
    /// <returns>
    /// <paramref name="builder" />.
    /// </returns>
    public static IConfigurationBuilder AddSpringBootFromCommandLine(this IConfigurationBuilder builder, IConfiguration configuration)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(configuration);

        builder.Add(new SpringBootCommandLineSource(configuration));

        return builder;
    }
}
