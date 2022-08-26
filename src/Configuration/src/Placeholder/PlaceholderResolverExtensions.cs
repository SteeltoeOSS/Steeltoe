// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;

namespace Steeltoe.Extensions.Configuration.Placeholder;

public static class PlaceholderResolverExtensions
{
    /// <summary>
    /// Creates a new <see cref="IConfiguration" /> using a <see cref="PlaceholderResolverProvider" /> which wraps the provided <see cref="IConfiguration" />
    /// . The new configuration will then be used to replace the current <see cref="IConfiguration" /> in the service container. All subsequent requests for
    /// a <see cref="IConfiguration" /> will return the newly created one providing placeholder resolution.
    /// </summary>
    /// <param name="services">
    /// The service container.
    /// </param>
    /// <param name="configuration">
    /// The configuration the placeholder resolver will wrap.
    /// </param>
    /// <returns>
    /// The new configuration.
    /// </returns>
    public static IConfiguration ConfigurePlaceholderResolver(this IServiceCollection services, IConfiguration configuration)
    {
        return ConfigurePlaceholderResolver(services, configuration, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Creates a new <see cref="IConfiguration" /> using a <see cref="PlaceholderResolverProvider" /> which wraps the provided <see cref="IConfiguration" />
    /// . The new configuration will then be used to replace the current <see cref="IConfiguration" /> in the service container. All subsequent requests for
    /// a <see cref="IConfiguration" /> will return the newly created one providing placeholder resolution.
    /// </summary>
    /// <param name="services">
    /// The service container.
    /// </param>
    /// <param name="configuration">
    /// The configuration the placeholder resolver will wrap.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The new configuration.
    /// </returns>
    public static IConfiguration ConfigurePlaceholderResolver(this IServiceCollection services, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(loggerFactory);

        IConfiguration newConfiguration = configuration.AddPlaceholderResolver(loggerFactory);
        services.Replace(ServiceDescriptor.Singleton(typeof(IConfiguration), newConfiguration));

        return newConfiguration;
    }

    /// <summary>
    /// Adds a placeholder resolver configuration source to the <see cref="ConfigurationBuilder" />. The placeholder resolver source will capture and wrap
    /// all the existing sources <see cref="IConfigurationSource" /> contained in the builder.  The newly created source will then replace the existing
    /// sources and provide placeholder resolution for the configuration. Typically you will want to add this configuration source as the last one so that
    /// you wrap all of the applications' configuration sources with placeholder resolution.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    /// <returns>
    /// The provided host builder.
    /// </returns>
    public static IWebHostBuilder AddPlaceholderResolver(this IWebHostBuilder hostBuilder)
    {
        return AddPlaceholderResolver(hostBuilder, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds a placeholder resolver configuration source to the <see cref="ConfigurationBuilder" />. The placeholder resolver source will capture and wrap
    /// all the existing sources <see cref="IConfigurationSource" /> contained in the builder.  The newly created source will then replace the existing
    /// sources and provide placeholder resolution for the configuration. Typically you will want to add this configuration source as the last one so that
    /// you wrap all of the applications' configuration sources with placeholder resolution.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The provided host builder.
    /// </returns>
    public static IWebHostBuilder AddPlaceholderResolver(this IWebHostBuilder hostBuilder, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(hostBuilder);
        ArgumentGuard.NotNull(loggerFactory);

        return hostBuilder.ConfigureAppConfiguration((_, builder) => builder.AddPlaceholderResolver(loggerFactory));
    }

    /// <summary>
    /// Adds a placeholder resolver configuration source to the <see cref="ConfigurationBuilder" />. The placeholder resolver source will capture and wrap
    /// all the existing sources <see cref="IConfigurationSource" /> contained in the builder.  The newly created source will then replace the existing
    /// sources and provide placeholder resolution for the configuration. Typically you will want to add this configuration source as the last one so that
    /// you wrap all of the applications configuration sources with placeholder resolution.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    /// <returns>
    /// The provided host builder.
    /// </returns>
    public static IHostBuilder AddPlaceholderResolver(this IHostBuilder hostBuilder)
    {
        return AddPlaceholderResolver(hostBuilder, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds a placeholder resolver configuration source to the <see cref="ConfigurationBuilder" />. The placeholder resolver source will capture and wrap
    /// all the existing sources <see cref="IConfigurationSource" /> contained in the builder.  The newly created source will then replace the existing
    /// sources and provide placeholder resolution for the configuration. Typically you will want to add this configuration source as the last one so that
    /// you wrap all of the applications configuration sources with placeholder resolution.
    /// </summary>
    /// <param name="hostBuilder">
    /// The host builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The provided host builder.
    /// </returns>
    public static IHostBuilder AddPlaceholderResolver(this IHostBuilder hostBuilder, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(hostBuilder);
        ArgumentGuard.NotNull(loggerFactory);

        return hostBuilder.ConfigureAppConfiguration((_, builder) => builder.AddPlaceholderResolver(loggerFactory));
    }

    /// <summary>
    /// Adds a placeholder resolver configuration source to the <see cref="ConfigurationBuilder" />. The placeholder resolver source will capture and wrap
    /// all the existing sources <see cref="IConfigurationSource" /> contained in the builder.  The newly created source will then replace the existing
    /// sources and provide placeholder resolution for the configuration. Typically you will want to add this configuration source as the last one so that
    /// you wrap all of the applications configuration sources with placeholder resolution.
    /// </summary>
    /// <param name="applicationBuilder">
    /// The application builder.
    /// </param>
    /// <returns>
    /// The provided application builder.
    /// </returns>
    public static WebApplicationBuilder AddPlaceholderResolver(this WebApplicationBuilder applicationBuilder)
    {
        return AddPlaceholderResolver(applicationBuilder, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds a placeholder resolver configuration source to the <see cref="ConfigurationBuilder" />. The placeholder resolver source will capture and wrap
    /// all the existing sources <see cref="IConfigurationSource" /> contained in the builder.  The newly created source will then replace the existing
    /// sources and provide placeholder resolution for the configuration. Typically you will want to add this configuration source as the last one so that
    /// you wrap all of the applications configuration sources with placeholder resolution.
    /// </summary>
    /// <param name="applicationBuilder">
    /// The application builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The provided application builder.
    /// </returns>
    public static WebApplicationBuilder AddPlaceholderResolver(this WebApplicationBuilder applicationBuilder, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(applicationBuilder);
        ArgumentGuard.NotNull(loggerFactory);

        applicationBuilder.Configuration.AddPlaceholderResolver(loggerFactory);
        return applicationBuilder;
    }
}
