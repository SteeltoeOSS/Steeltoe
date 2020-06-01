// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Hosting
{
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Configure the application to listen on port(s) provided by the environment at runtime. Defaults to port 8080.
        /// </summary>
        /// <param name="webHostBuilder">Your WebHostBuilder</param>
        /// <param name="runLocalHttpPort">Set the Http port number with code so you don't need to set environment variables locally</param>
        /// <param name="runLocalHttpsPort">Set the Https port number with code so you don't need to set environment variables locally</param>
        /// <returns>Your HostBuilder, now listening on port(s) found in the environment or passed in</returns>
        /// <remarks>runLocalPort parameter will not be used if an environment variable PORT is found</remarks>
        public static IWebHostBuilder UseCloudHosting(this IWebHostBuilder webHostBuilder, int? runLocalHttpPort = null, int? runLocalHttpsPort = null)
        {
            if (webHostBuilder == null)
            {
                throw new ArgumentNullException(nameof(webHostBuilder));
            }

            return webHostBuilder.BindToPorts(runLocalHttpPort, runLocalHttpsPort);
        }

#if NETCOREAPP3_0
        /// <summary>
        /// Configure the application to listen on port(s) provided by the environment at runtime. Defaults to port 8080.
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        /// <param name="runLocalHttpPort">Set the Http port number with code so you don't need to set environment variables locally</param>
        /// <param name="runLocalHttpsPort">Set the Https port number with code so you don't need to set environment variables locally</param>
        /// <returns>Your HostBuilder, now listening on port(s) found in the environment or passed in</returns>
        /// <remarks>runLocalPort parameter will not be used if an environment variable PORT is found</remarks>
        public static IHostBuilder UseCloudHosting(this IHostBuilder hostBuilder, int? runLocalHttpPort = null, int? runLocalHttpsPort = null)
        {
            if (hostBuilder == null)
            {
                throw new ArgumentNullException(nameof(hostBuilder));
            }

            return hostBuilder.ConfigureWebHost(configure => configure.BindToPorts(runLocalHttpPort, runLocalHttpsPort));
        }
#endif

        private static IWebHostBuilder BindToPorts(this IWebHostBuilder webHostBuilder, int? runLocalHttpPort, int? runLocalHttpsPort)
        {
            var urls = new List<string>();

            var portStr = Environment.GetEnvironmentVariable("PORT") ?? Environment.GetEnvironmentVariable("SERVER_PORT");
            if (!string.IsNullOrWhiteSpace(portStr))
            {
                if (int.TryParse(portStr, out var port))
                {
                    urls.Add($"http://*:{port}");
                }
            }
            else
            {
                if (runLocalHttpPort != null)
                {
                    urls.Add($"http://*:{runLocalHttpPort}");
                }

                if (runLocalHttpsPort != null)
                {
                    urls.Add($"https://*:{runLocalHttpsPort}");
                }
            }

            if (urls.Count > 0)
            {
                webHostBuilder.UseUrls(urls.ToArray());
            }
            else
            {
                webHostBuilder.UseUrls(new string[] { "http://*:8080" });
            }

            return webHostBuilder;
        }
    }
}
