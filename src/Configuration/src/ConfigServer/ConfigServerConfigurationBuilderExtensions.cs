// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;
using Steeltoe.Extensions.Configuration.CloudFoundry;

namespace Steeltoe.Extensions.Configuration.ConfigServer;

/// <summary>
/// Extension methods for adding <see cref="ConfigServerConfigurationProvider" />.
/// </summary>
public static class ConfigServerConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder)
    {
        return AddConfigServer(configurationBuilder, NullLoggerFactory.Instance);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, ILoggerFactory loggerFactory)
    {
        return AddConfigServer(configurationBuilder, ConfigServerClientSettings.DefaultEnvironment, loggerFactory);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, string environment)
    {
        return AddConfigServer(configurationBuilder, environment, NullLoggerFactory.Instance);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, string environment, ILoggerFactory loggerFactory)
    {
        string applicationName = Assembly.GetEntryAssembly()?.GetName().Name;
        return AddConfigServer(configurationBuilder, environment, applicationName, loggerFactory);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, string environment, string applicationName)
    {
        return AddConfigServer(configurationBuilder, environment, applicationName, NullLoggerFactory.Instance);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, string environment, string applicationName,
        ILoggerFactory loggerFactory)
    {
        var settings = new ConfigServerClientSettings
        {
            Name = applicationName ?? Assembly.GetEntryAssembly()?.GetName().Name,
            Environment = environment ?? ConfigServerClientSettings.DefaultEnvironment
        };

        return AddConfigServer(configurationBuilder, settings, loggerFactory);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, ConfigServerClientSettings clientSettings)
    {
        return AddConfigServer(configurationBuilder, clientSettings, NullLoggerFactory.Instance);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, IHostEnvironment environment)
    {
        return AddConfigServer(configurationBuilder, environment, NullLoggerFactory.Instance);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, IHostEnvironment environment,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(environment);

        var settings = new ConfigServerClientSettings
        {
            Name = environment.ApplicationName,
            Environment = environment.EnvironmentName
        };

        return AddConfigServer(configurationBuilder, settings, loggerFactory);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, ConfigServerClientSettings clientSettings,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(configurationBuilder);
        ArgumentGuard.NotNull(clientSettings);
        ArgumentGuard.NotNull(loggerFactory);

        if (configurationBuilder.Sources.All(source => source is not CloudFoundryConfigurationSource))
        {
            configurationBuilder.Add(new CloudFoundryConfigurationSource());
        }

        if (configurationBuilder is IConfiguration configuration)
        {
            var source = new ConfigServerConfigurationSource(clientSettings, configuration, loggerFactory);
            configurationBuilder.Add(source);
        }
        else
        {
            var source = new ConfigServerConfigurationSource(clientSettings, configurationBuilder.Sources, configurationBuilder.Properties, loggerFactory);
            configurationBuilder.Add(source);
        }

        return configurationBuilder;
    }
}
