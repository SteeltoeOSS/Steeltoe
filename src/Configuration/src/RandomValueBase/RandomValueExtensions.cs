// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace Steeltoe.Extensions.Configuration.RandomValue;

public static class RandomValueExtensions
{
    /// <summary>
    /// Add a random value configuration source to the <see cref="ConfigurationBuilder"/>.
    /// </summary>
    /// <param name="builder">the configuration builder.</param>
    /// <param name="loggerFactory">the logger factory to use.</param>
    /// <returns>builder.</returns>
    public static IConfigurationBuilder AddRandomValueSource(this IConfigurationBuilder builder, ILoggerFactory loggerFactory = null)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var resolver = new RandomValueSource(loggerFactory);
        builder.Add(resolver);

        return builder;
    }

    /// <summary>
    /// Add a random value configuration source to the <see cref="ConfigurationBuilder"/>.
    /// </summary>
    /// <param name="builder">the configuration builder.</param>
    /// <param name="prefix">the prefix used for random key values, default 'random:'.</param>
    /// <param name="loggerFactory">the logger factory to use.</param>
    /// <returns>builder.</returns>
    public static IConfigurationBuilder AddRandomValueSource(this IConfigurationBuilder builder, string prefix, ILoggerFactory loggerFactory = null)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrEmpty(prefix))
        {
            throw new ArgumentException(nameof(prefix));
        }

        if (!prefix.EndsWith(":"))
        {
            prefix += ":";
        }

        var resolver = new RandomValueSource(prefix, loggerFactory);
        builder.Add(resolver);

        return builder;
    }
}
