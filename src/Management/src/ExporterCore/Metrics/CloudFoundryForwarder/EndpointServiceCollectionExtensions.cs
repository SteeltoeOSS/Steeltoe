﻿// Copyright 2017 the original author or authors.
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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Extensions.Configuration;
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

            services.TryAddSingleton(new CloudFoundryForwarderOptions(services.GetApplicationInstanceInfo(), services.GetServicesInfo(), config));
            services.TryAddSingleton<IMetricsExporter, CloudFoundryForwarderExporter>();
        }
    }
}
