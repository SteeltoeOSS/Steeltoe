// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;

namespace Steeltoe.Extensions.Configuration.RandomValue;

public static class RandomValueExtensions
{
    /// <summary>
    /// Add a random value configuration source to the <see cref="ConfigurationBuilder" />.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" />.
    /// </returns>
    public static IConfigurationBuilder AddRandomValueSource(this IConfigurationBuilder builder)
    {
        return AddRandomValueSource(builder, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Add a random value configuration source to the <see cref="ConfigurationBuilder" />.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" />.
    /// </returns>
    public static IConfigurationBuilder AddRandomValueSource(this IConfigurationBuilder builder, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(loggerFactory);

        var resolver = new RandomValueSource(loggerFactory);
        builder.Add(resolver);

        return builder;
    }

    /// <summary>
    /// Add a random value configuration source to the <see cref="ConfigurationBuilder" /> using a custom prefix for key values.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder.
    /// </param>
    /// <param name="prefix">
    /// The prefix used for random key values.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" />.
    /// </returns>
    public static IConfigurationBuilder AddRandomValueSource(this IConfigurationBuilder builder, string prefix)
    {
        return AddRandomValueSource(builder, prefix, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Add a random value configuration source to the <see cref="ConfigurationBuilder" /> using a custom prefix for key values.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder.
    /// </param>
    /// <param name="prefix">
    /// The prefix used for random key values.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" />.
    /// </returns>
    public static IConfigurationBuilder AddRandomValueSource(this IConfigurationBuilder builder, string prefix, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNullOrEmpty(prefix);
        ArgumentGuard.NotNull(loggerFactory);

        if (!prefix.EndsWith(':'))
        {
            prefix += ':';
        }

        var source = new RandomValueSource(prefix, loggerFactory);
        builder.Add(source);

        return builder;
    }
}
