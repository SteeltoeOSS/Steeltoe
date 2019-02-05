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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Owin;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Discovery;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.EndpointOwin.Diagnostics;
using Steeltoe.Management.EndpointOwin.Discovery;
using Steeltoe.Management.EndpointOwin.Health;
using Steeltoe.Management.EndpointOwin.Info;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Description;

namespace Steeltoe.Management.EndpointOwin
{
    public static class ActuatorAppBuilderExtensions
    {
        /// <summary>
        /// Add all Actuators (Discovery, Info, Health )
        /// </summary>
        /// <param name="app">AppBuilder needing actuators added</param>
        /// <param name="configuration">configuration to use for actuators</param>
        /// <param name="apiExplorer">a IApiExplorer to use for mappings actuator</param>
        /// <param name="loggerProvider">the Steeltoe logging provider to use for loggers actuator</param>
        /// <param name="loggerFactory">logging factory used to create loggers for the actuators</param>
        public static void UseDiscoveryActuators(this IAppBuilder app, IConfiguration configuration, IApiExplorer apiExplorer,  ILoggerProvider loggerProvider, ILoggerFactory loggerFactory = null)
        {
            app.UseDiagnosticSourceMiddleware(loggerFactory);
            app.UseDiscoveryActuator(configuration, loggerFactory);

            app.UseInfoActuator(configuration, loggerFactory);
            app.UseHealthActuator(configuration, loggerFactory);

        }

        /// <summary>
        /// Add all Actuators (Discovery, Info, Health )
        /// </summary>
        /// <param name="app">AppBuilder needing actuators added</param>
        /// <param name="configuration">configuration to use for actuators</param>
        /// <param name="healthContributors">custom health contributors</param>
        /// <param name="apiExplorer">a IApiExplorer to use for mappings actuator</param>
        /// <param name="loggerProvider">the Steeltoe logging provider to use for loggers actuator</param>
        /// <param name="loggerFactory">logging factory used to create loggers for the actuators</param>
        public static void UseDiscoveryActuators(this IAppBuilder app, IConfiguration configuration, IEnumerable<IHealthContributor> healthContributors, IApiExplorer apiExplorer, ILoggerProvider loggerProvider, ILoggerFactory loggerFactory = null)
        {
            app.UseDiagnosticSourceMiddleware(loggerFactory);

            var mgmtOptions = ManagementOptions.Get(configuration);
            app.UseActuatorSecurityMiddleware(configuration, loggerFactory);
            app.UseDiscoveryActuator(configuration, loggerFactory);

            app.UseInfoActuator(configuration, loggerFactory);
            var healthOptions = new HealthEndpointOptions(configuration);
            app.UseHealthActuator(healthOptions, new DefaultHealthAggregator(), healthContributors, loggerFactory);
        }
    }
}
