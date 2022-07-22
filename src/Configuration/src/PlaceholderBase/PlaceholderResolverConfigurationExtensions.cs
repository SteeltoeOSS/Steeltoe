// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration.Placeholder;

public static class PlaceholderResolverConfigurationExtensions
{
#if NET6_0_OR_GREATER

        /// <summary>
        /// Add a placeholder resolver configuration source to the <see cref="ConfigurationBuilder"/>. The placeholder resolver source will capture and wrap all
        /// the existing sources <see cref="IConfigurationSource"/> contained in the builder.  The newly created source will then replace the existing sources
        /// and provide placeholder resolution for the configuration. Typically you will want to add this configuration source as the last one so that you wrap all
        /// of the applications configuration sources with place holder resolution.
        /// </summary>
        /// <param name="configuration">the ConfigurationManager</param>
        /// <param name="loggerFactory">the logger factory to use</param>
        /// <returns>builder</returns>
        public static ConfigurationManager AddPlaceholderResolver(this ConfigurationManager configuration, ILoggerFactory loggerFactory = null)
        {
            (configuration as IConfigurationBuilder).AddPlaceholderResolver(loggerFactory);

            return configuration;
        }
#endif

    /// <summary>
    /// Add a placeholder resolver configuration source to the <see cref="ConfigurationBuilder"/>. The placeholder resolver source will capture and wrap all
    /// the existing sources <see cref="IConfigurationSource"/> contained in the builder.  The newly created source will then replace the existing sources
    /// and provide placeholder resolution for the configuration. Typically you will want to add this configuration source as the last one so that you wrap all
    /// of the applications configuration sources with place holder resolution.
    /// </summary>
    /// <param name="builder">the configuration builder</param>
    /// <param name="loggerFactory">the logger factory to use</param>
    /// <returns>builder</returns>
    public static IConfigurationBuilder AddPlaceholderResolver(this IConfigurationBuilder builder, ILoggerFactory loggerFactory = null)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (builder is IConfigurationRoot configuration)
        {
            builder.Add(new PlaceholderResolverSource(configuration, loggerFactory));
        }
        else
        {
            var resolver = new PlaceholderResolverSource(builder.Sources, loggerFactory);
            builder.Sources.Clear();
            builder.Add(resolver);
        }

        return builder;
    }

    /// <summary>
    /// Creates a new <see cref="ConfigurationRoot"/> from a <see cref="PlaceholderResolverProvider"/>.  The place holder resolver will be created using the existing
    /// configuration providers contained in the incoming configuration.  This results in providing placeholder resolution for those configuration sources.
    /// </summary>
    /// <param name="configuration">incoming configuration to wrap</param>
    /// <param name="loggerFactory">the logger factory to use</param>
    /// <returns>a new configuration</returns>
    public static IConfiguration AddPlaceholderResolver(this IConfiguration configuration, ILoggerFactory loggerFactory = null)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var root = configuration as IConfigurationRoot;
        return new ConfigurationRoot(new List<IConfigurationProvider>() { new PlaceholderResolverProvider(new List<IConfigurationProvider>(root.Providers), loggerFactory) });
    }
}