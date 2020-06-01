// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            credHubOptions.Validate();

            CredHubClient credHubClient;
            try
            {
                startupLogger?.LogTrace("Using UAA auth for CredHub client with client id {ClientId}", credHubOptions.ClientId);
                credHubClient = CredHubClient.CreateUAAClientAsync(credHubOptions, credhubLogger).GetAwaiter().GetResult();

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
