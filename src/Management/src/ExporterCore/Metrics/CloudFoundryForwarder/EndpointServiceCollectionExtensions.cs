// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder;
using System;

namespace Steeltoe.Management.Exporter.Metrics
{
    public static class EndpointServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Cloud Foundry metrics exporter
        /// </summary>
        /// <param name="services">Service collection to add exporter to</param>
        /// <param name="config">Application configuration (this actuator looks for a settings starting with management:endpoints:metrics)</param>
        public static void AddMetricsForwarderExporter(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.TryAddSingleton(new CloudFoundryForwarderOptions(config));
            services.TryAddSingleton<IMetricsExporter, CloudFoundryForwarderExporter>();
        }
    }
}
