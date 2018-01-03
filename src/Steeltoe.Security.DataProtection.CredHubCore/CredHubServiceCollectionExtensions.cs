// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Security.DataProtection.CredHub;
using System;

namespace Steeltoe.Security.DataProtection.CredHubCore
{
    public static class CredHubServiceCollectionExtensions
    {
        /// <summary>
        /// Make a CredHubClient available to DI
        /// </summary>
        /// <remarks>Uses UAA user/password authentication if configured, otherwise mTLS</remarks>
        /// <param name="services">Service collection</param>
        /// <param name="config">App configuration</param>
        /// <param name="loggerFactory">Logger factory</param>
        /// <returns>Service collection with CredHubClient added in</returns>
        public static IServiceCollection AddCredHubClient(this IServiceCollection services, IConfiguration config, ILoggerFactory loggerFactory = null)
        {
            ILogger startupLogger = null;
            ILogger credhubLogger = null;
            if (loggerFactory != null)
            {
                startupLogger = loggerFactory.CreateLogger("Steeltoe.Security.DataProtection.CredHubCore");
                credhubLogger = loggerFactory.CreateLogger<CredHubClient>();
            }

            var credHubOptions = config.GetSection("CredHubClient").Get<CredHubOptions>();
            CredHubClient credHubClient;
            try
            {
                // if a username and password were supplied, use that auth method, otherwise expect Diego to provide credentials on PCF
                if (!string.IsNullOrEmpty(credHubOptions?.CredHubUser) && !string.IsNullOrEmpty(credHubOptions?.CredHubPassword))
                {
                    startupLogger?.LogTrace("Using UAA auth for CredHub client");
                    credHubClient = CredHubClient.CreateUAAClientAsync(credHubOptions).Result;
                }
                else
                {
                    startupLogger?.LogTrace("Using mTLS auth for CredHub client");
                    credHubClient = CredHubClient.CreateMTLSClientAsync(credHubOptions ?? new CredHubOptions()).Result;
                }

                services.AddSingleton<ICredHubClient>(credHubClient);
            }
            catch (Exception e)
            {
                startupLogger?.LogCritical(e, "Failed to initialize CredHub client for ServiceCollection");
            }

            return services;
        }
    }
}
