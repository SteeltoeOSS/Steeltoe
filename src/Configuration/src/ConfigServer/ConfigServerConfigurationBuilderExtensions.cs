// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Configuration.Kubernetes.ServiceBinding;
using Steeltoe.Configuration.Placeholder;

namespace Steeltoe.Configuration.ConfigServer;

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
        return AddConfigServer(configurationBuilder, "Production", loggerFactory);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, string? environment)
    {
        return AddConfigServer(configurationBuilder, environment, NullLoggerFactory.Instance);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, string? environment, ILoggerFactory loggerFactory)
    {
        string? applicationName = Assembly.GetEntryAssembly()?.GetName().Name;
        return AddConfigServer(configurationBuilder, environment, applicationName, loggerFactory);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, string? environment, string? applicationName)
    {
        return AddConfigServer(configurationBuilder, environment, applicationName, NullLoggerFactory.Instance);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, string? environment, string? applicationName,
        ILoggerFactory loggerFactory)
    {
        var options = new ConfigServerClientOptions
        {
            Name = applicationName ?? Assembly.GetEntryAssembly()?.GetName().Name,
            Environment = environment ?? "Production"
        };

        return AddConfigServer(configurationBuilder, options, loggerFactory);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, ConfigServerClientOptions options)
    {
        return AddConfigServer(configurationBuilder, options, NullLoggerFactory.Instance);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, IHostEnvironment environment)
    {
        return AddConfigServer(configurationBuilder, environment, NullLoggerFactory.Instance);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, IHostEnvironment environment,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(environment);

        var options = new ConfigServerClientOptions
        {
            Name = environment.ApplicationName,
            Environment = environment.EnvironmentName
        };

        return AddConfigServer(configurationBuilder, options, loggerFactory);
    }

    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, ConfigServerClientOptions options,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        if (configurationBuilder.Sources.All(source => source is not CloudFoundryConfigurationSource))
        {
            configurationBuilder.AddCloudFoundry();
        }

        if (configurationBuilder.Sources.All(source => source is not KubernetesServiceBindingConfigurationSource))
        {
            configurationBuilder.AddKubernetesServiceBindings();
        }

        if (configurationBuilder.Sources.All(source => source is not PlaceholderResolverSource))
        {
            configurationBuilder.AddPlaceholderResolver(loggerFactory);
        }

        if (configurationBuilder is IConfiguration configuration)
        {
            var source = new ConfigServerConfigurationSource(options, configuration, loggerFactory);
            configurationBuilder.Add(source);
        }
        else
        {
            var source = new ConfigServerConfigurationSource(options, configurationBuilder.Sources, configurationBuilder.Properties, loggerFactory);
            configurationBuilder.Add(source);
        }

        return configurationBuilder;
    }
}
