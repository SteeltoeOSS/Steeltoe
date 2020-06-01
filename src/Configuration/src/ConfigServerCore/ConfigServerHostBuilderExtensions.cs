// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

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

        /// <summary>
        /// Enable the application to listen on port(s) provided by the environment at runtime
        /// </summary>
        /// <param name="webHostBuilder">Your WebHostBuilder</param>
        /// <param name="runLocalPort">Set the port number with code so you don't need to set environment variables locally</param>
        /// <returns>Your WebHostBuilder, now listening on port(s) found in the environment or passed in</returns>
        /// <remarks>runLocalPort parameter will not be used if an environment variable PORT is found</remarks>
        [Obsolete("This extension will be removed in a future release. Please use Steeltoe.Common.Hosting.UseCloudHosting() instead")]
        public static IWebHostBuilder UseCloudFoundryHosting(this IWebHostBuilder webHostBuilder, int? runLocalPort = null)
        {
            if (webHostBuilder == null)
            {
                throw new ArgumentNullException(nameof(webHostBuilder));
            }

            List<string> urls = new List<string>();

            string portStr = Environment.GetEnvironmentVariable("PORT");
            if (!string.IsNullOrWhiteSpace(portStr))
            {
                if (int.TryParse(portStr, out int port))
                {
                    urls.Add($"http://*:{port}");
                }
            }
            else if (runLocalPort != null)
            {
                urls.Add($"http://*:{runLocalPort}");
            }

            if (urls.Count > 0)
            {
                webHostBuilder.UseUrls(urls.ToArray());
            }

            return webHostBuilder;
        }
    }
}
