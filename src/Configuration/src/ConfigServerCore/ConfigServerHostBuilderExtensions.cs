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
    }
}
