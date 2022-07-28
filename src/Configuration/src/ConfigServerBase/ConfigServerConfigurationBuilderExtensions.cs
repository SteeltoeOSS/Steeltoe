// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Linq;
using System.Reflection;

namespace Steeltoe.Extensions.Configuration.ConfigServer;

public static class ConfigServerConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, ILoggerFactory logFactory = null)
    {
        return configurationBuilder.AddConfigServer(ConfigServerClientSettings.DefaultEnvironment, Assembly.GetEntryAssembly()?.GetName().Name, logFactory);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, string environment, ILoggerFactory logFactory = null)
    {
        return configurationBuilder.AddConfigServer(environment, Assembly.GetEntryAssembly()?.GetName().Name, logFactory);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, string environment, string applicationName, ILoggerFactory logFactory = null)
    {
        if (configurationBuilder == null)
        {
            throw new ArgumentNullException(nameof(configurationBuilder));
        }

        var settings = new ConfigServerClientSettings
        {
            Name = applicationName ?? Assembly.GetEntryAssembly()?.GetName().Name,

            Environment = environment ?? ConfigServerClientSettings.DefaultEnvironment
        };

        return configurationBuilder.AddConfigServer(settings, logFactory);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, ConfigServerClientSettings defaultSettings, ILoggerFactory logFactory = null)
    {
        if (configurationBuilder == null)
        {
            throw new ArgumentNullException(nameof(configurationBuilder));
        }

        if (defaultSettings == null)
        {
            throw new ArgumentNullException(nameof(defaultSettings));
        }

        if (!configurationBuilder.Sources.Any(c => c.GetType() == typeof(CloudFoundryConfigurationSource)))
        {
            configurationBuilder.Add(new CloudFoundryConfigurationSource());
        }

        if (configurationBuilder is IConfiguration configuration)
        {
            configurationBuilder.Add(new ConfigServerConfigurationSource(defaultSettings, configuration, logFactory));
        }
        else
        {
            configurationBuilder.Add(new ConfigServerConfigurationSource(defaultSettings, configurationBuilder.Sources, configurationBuilder.Properties, logFactory));
        }

        return configurationBuilder;
    }
}
