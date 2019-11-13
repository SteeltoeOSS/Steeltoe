// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
