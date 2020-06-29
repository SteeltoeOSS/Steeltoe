// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint.Hypermedia;
using System;

namespace Steeltoe.Management.Endpoint.Loggers
{
    public static class EndpointServiceCollectionExtensions
    {
        /// <summary>
        /// Adds components of the Loggers actuator to Microsoft-DI
        /// </summary>
        /// <param name="services">Service collection to add logging to</param>
        /// <param name="config">Application configuration (this actuator looks for a settings starting with management:endpoints:loggers)</param>
        public static void AddLoggersActuator(this IServiceCollection services, IConfiguration config = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            config ??= services.BuildServiceProvider().GetRequiredService<IConfiguration>();

            services.AddActuatorManagementOptions(config);

            var options = new LoggersEndpointOptions(config);
            services.TryAddSingleton<ILoggersOptions>(options);
            services.RegisterEndpointOptions(options);
            services.TryAddSingleton<LoggersEndpoint>();
        }
    }
}
