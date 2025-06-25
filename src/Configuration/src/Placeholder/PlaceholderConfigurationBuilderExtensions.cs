// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Steeltoe.Configuration.Placeholder;

public static class PlaceholderConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds a configuration source to the <see cref="ConfigurationBuilder" /> that substitutes placeholders in configuration values.
    /// <para>
    /// This method replaces all the existing <see cref="IConfigurationSource" />s contained in the builder, taking ownership of them. The newly added source
    /// then provides placeholder substitution of values stored in its inner configuration (built from the pre-existing sources). Typically, you will want to
    /// add this configuration source as the last one, so that placeholder substitution is applied to all configuration values.
    /// </para>
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddPlaceholderResolver(this IConfigurationBuilder builder)
    {
        return AddPlaceholderResolver(builder, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds a configuration source to the <see cref="ConfigurationBuilder" /> that substitutes placeholders in configuration values.
    /// <para>
    /// This method replaces all the existing <see cref="IConfigurationSource" />s contained in the builder, taking ownership of them. The newly added source
    /// then provides placeholder substitution of values stored in its inner configuration (built from the pre-existing sources). Typically, you will want to
    /// add this configuration source as the last one, so that placeholder substitution is applied to all configuration values.
    /// </para>
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
    public static IConfigurationBuilder AddPlaceholderResolver(this IConfigurationBuilder builder, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var placeholderSource = new PlaceholderConfigurationSource(loggerFactory);

        foreach (IConfigurationSource source in builder.Sources)
        {
            placeholderSource.Sources.Add(source);
        }

        builder.Sources.Clear();
        builder.Sources.Add(placeholderSource);

        return builder;
    }
}
