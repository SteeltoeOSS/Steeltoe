﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Extensions.Configuration.ConfigServer
{
    public static class ConfigServerHostBuilderExtensions
    {
        /// <summary>
        /// Add Config Server and Cloud Foundry as application configuration sources. Add Config Server health check
        /// contributor to the service container.
        /// </summary>
        /// <param name="hostBuilder"><see cref="IWebHostBuilder"/></param>
        /// <param name="loggerFactory"><see cref="ILoggerFactory"/></param>
        /// <returns><see cref="IWebHostBuilder"/> with config server and Cloud Foundry Config Provider attached</returns>
        public static IWebHostBuilder AddConfigServer(this IWebHostBuilder hostBuilder, ILoggerFactory loggerFactory = null)
        {
            hostBuilder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddConfigServer(context.HostingEnvironment, loggerFactory);
            });

            hostBuilder.ConfigureServices((services) =>
            {
                services.AddConfigServerHealthContributor();
            });

            return hostBuilder;
        }

        /// <summary>
        /// Add Config Server and Cloud Foundry as application configuration sources. Add Config Server health check
        /// contributor to the service container.
        /// </summary>
        /// <param name="hostBuilder"><see cref="IHostBuilder"/></param>
        /// <param name="loggerFactory"><see cref="ILoggerFactory"/></param>
        /// <returns><see cref="IHostBuilder"/> with config server and Cloud Foundry Config Provider attached</returns>
        public static IHostBuilder AddConfigServer(this IHostBuilder hostBuilder, ILoggerFactory loggerFactory = null)
        {
            hostBuilder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddConfigServer(context.HostingEnvironment, loggerFactory);
            });

            hostBuilder.ConfigureServices((services) =>
            {
                services.AddConfigServerHealthContributor();
            });

            return hostBuilder;
        }
    }
}
