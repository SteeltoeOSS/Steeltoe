// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    public static class CloudFoundryHostBuilderExtensions
    {
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

#if NETCOREAPP3_0
        /// <summary>
        /// Enable the application to listen on port(s) provided by the environment at runtime
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        /// <param name="runLocalPort">Set the port number with code so you don't need to set environment variables locally</param>
        /// <returns>Your HostBuilder, now listening on port(s) found in the environment or passed in</returns>
        /// <remarks>runLocalPort parameter will not be used if an environment variable PORT is found</remarks>
        [Obsolete("This extension will be removed in a future release. Please use Steeltoe.Common.Hosting.UseCloudHosting() instead")]
        public static IHostBuilder UseCloudFoundryHosting(this IHostBuilder hostBuilder, int? runLocalPort = null)
        {
            if (hostBuilder == null)
            {
                throw new ArgumentNullException(nameof(hostBuilder));
            }

            var urls = new List<string>();

            var portStr = Environment.GetEnvironmentVariable("PORT");
            if (!string.IsNullOrWhiteSpace(portStr))
            {
                if (int.TryParse(portStr, out var port))
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
                hostBuilder.ConfigureWebHost(configure => configure.UseUrls(urls.ToArray()));
            }

            return hostBuilder;
        }
#endif

        /// <summary>
        /// Add Cloud Foundry Configuration Provider
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        public static IWebHostBuilder AddCloudFoundry(this IWebHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddCloudFoundry();
            });

            return hostBuilder;
        }

        /// <summary>
        /// Add Cloud Foundry Configuration Provider
        /// </summary>
        /// <param name="hostBuilder">Your WebHostBuilder</param>
        public static IHostBuilder AddCloudFoundry(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddCloudFoundry();
            });

            return hostBuilder;
        }
    }
}
