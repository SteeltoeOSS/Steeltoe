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
using System;

namespace Steeltoe.Extensions.Configuration.Placeholder;

public static class PlaceholderResolverExtensions
{
    /// <summary>
    /// Create a new <see cref="IConfiguration"/> using a <see cref="PlaceholderResolverProvider"/> which wraps the provided <see cref="IConfiguration"/>.
    /// The new configuration will then be used to replace the current <see cref="IConfiguration"/> in the service container. All subsequent requests for a
    /// <see cref="IConfiguration"/> will return the newly created one providing placeholder resolution.
    /// </summary>
    /// <param name="services">the current service container</param>
    /// <param name="configuration">the configuration the placeholder resolver will wrap</param>
    /// <param name="loggerFactory">the log factory to use</param>
    /// <returns>the new configuration</returns>
    public static IConfiguration ConfigurePlaceholderResolver(this IServiceCollection services, IConfiguration configuration, ILoggerFactory loggerFactory = null)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var newConfig = configuration.AddPlaceholderResolver(loggerFactory);

        services.Replace(ServiceDescriptor.Singleton(typeof(IConfiguration), newConfig));

        return newConfig;
    }

    /// <summary>
    /// Add a placeholder resolver configuration source to the <see cref="ConfigurationBuilder"/>. The placeholder resolver source will capture and wrap all
    /// the existing sources <see cref="IConfigurationSource"/> contained in the builder.  The newly created source will then replace the existing sources
    /// and provide placeholder resolution for the configuration. Typically you will want to add this configuration source as the last one so that you wrap all
    /// of the applications configuration sources with place holder resolution.
    /// </summary>
    /// <param name="hostBuilder">the host builder</param>
    /// <param name="loggerFactory">the log factory to use</param>
    /// <returns>provided host builder</returns>
    public static IWebHostBuilder AddPlaceholderResolver(this IWebHostBuilder hostBuilder, ILoggerFactory loggerFactory = null) =>
        hostBuilder.ConfigureAppConfiguration((context, builder) => builder.AddPlaceholderResolver(loggerFactory));

    /// <summary>
    /// Add a placeholder resolver configuration source to the <see cref="ConfigurationBuilder"/>. The placeholder resolver source will capture and wrap all
    /// the existing sources <see cref="IConfigurationSource"/> contained in the builder.  The newly created source will then replace the existing sources
    /// and provide placeholder resolution for the configuration. Typically you will want to add this configuration source as the last one so that you wrap all
    /// of the applications configuration sources with place holder resolution.
    /// </summary>
    /// <param name="hostBuilder">the host builder</param>
    /// <param name="loggerFactory">the log factory to use</param>
    /// <returns>provided host builder</returns>
    public static IHostBuilder AddPlaceholderResolver(this IHostBuilder hostBuilder, ILoggerFactory loggerFactory = null) =>
        hostBuilder.ConfigureAppConfiguration((context, builder) => builder.AddPlaceholderResolver(loggerFactory));

#if NET6_0_OR_GREATER
    /// <summary>
    /// Add a placeholder resolver configuration source to the <see cref="ConfigurationBuilder"/>. The placeholder resolver source will capture and wrap all
    /// the existing sources <see cref="IConfigurationSource"/> contained in the builder.  The newly created source will then replace the existing sources
    /// and provide placeholder resolution for the configuration. Typically you will want to add this configuration source as the last one so that you wrap all
    /// of the applications configuration sources with place holder resolution.
    /// </summary>
    /// <param name="applicationBuilder">Your <see cref="WebApplicationBuilder"/></param>
    /// <param name="loggerFactory">the log factory to use</param>
    /// <returns>provided host builder</returns>
    public static WebApplicationBuilder AddPlaceholderResolver(this WebApplicationBuilder applicationBuilder, ILoggerFactory loggerFactory = null)
    {
        applicationBuilder.Configuration.AddPlaceholderResolver(loggerFactory);
        return applicationBuilder;
    }
#endif
}