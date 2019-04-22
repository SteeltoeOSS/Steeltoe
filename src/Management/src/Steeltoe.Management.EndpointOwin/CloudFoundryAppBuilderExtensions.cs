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
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.EndpointOwin.CloudFoundry;
using Steeltoe.Management.EndpointOwin.Diagnostics;
using Steeltoe.Management.EndpointOwin.Health;
using Steeltoe.Management.EndpointOwin.HeapDump;
using Steeltoe.Management.EndpointOwin.Info;
using Steeltoe.Management.EndpointOwin.Loggers;
using Steeltoe.Management.EndpointOwin.Mappings;
using Steeltoe.Management.EndpointOwin.ThreadDump;
using Steeltoe.Management.EndpointOwin.Trace;
using System;
using System.Collections.Generic;
using System.Web.Http.Description;

namespace Steeltoe.Management.EndpointOwin
{
    public static class CloudFoundryAppBuilderExtensions
    {
        /// <summary>
        /// Add all CloudFoundry Actuators (Info, Health, Loggers, Trace)
        /// </summary>
        /// <param name="app">AppBuilder needing actuators added</param>
        /// <param name="configuration">configuration to use for actuators</param>
        /// <param name="apiExplorer">a IApiExplorer to use for mappings actuator</param>
        /// <param name="loggerProvider">the Steeltoe logging provider to use for loggers actuator</param>
        /// <param name="loggerFactory">logging factory used to create loggers for the actuators</param>
        public static void UseCloudFoundryActuators(this IAppBuilder app, IConfiguration configuration, IApiExplorer apiExplorer,  ILoggerProvider loggerProvider, ILoggerFactory loggerFactory = null)
        {
            app.UseDiagnosticSourceMiddleware(loggerFactory);
            app.UseCloudFoundrySecurityMiddleware(configuration, loggerFactory);
            app.UseCloudFoundryActuator(configuration, loggerFactory);

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                app.UseThreadDumpActuator(configuration, loggerFactory);
                app.UseHeapDumpActuator(configuration, null, loggerFactory);
            }

            app.UseInfoActuator(configuration, loggerFactory);
            app.UseHealthActuator(configuration, loggerFactory);
            app.UseLoggersActuator(configuration, loggerProvider, loggerFactory);
            app.UseTraceActuator(configuration, null, loggerFactory);
            app.UseMappingActuator(configuration, apiExplorer, loggerFactory);
        }

        /// <summary>
        /// Add all Cloud Foundry Actuators (Info, Health, Loggers, Trace)
        /// </summary>
        /// <param name="app">AppBuilder needing actuators added</param>
        /// <param name="configuration">configuration to use for actuators</param>
        /// <param name="healthContributors">custom health contributors</param>
        /// <param name="apiExplorer">a IApiExplorer to use for mappings actuator</param>
        /// <param name="loggerProvider">the Steeltoe logging provider to use for loggers actuator</param>
        /// <param name="loggerFactory">logging factory used to create loggers for the actuators</param>
        public static void UseCloudFoundryActuators(this IAppBuilder app, IConfiguration configuration, IEnumerable<IHealthContributor> healthContributors, IApiExplorer apiExplorer, ILoggerProvider loggerProvider, ILoggerFactory loggerFactory = null)
        {
            app.UseDiagnosticSourceMiddleware(loggerFactory);
            app.UseCloudFoundrySecurityMiddleware(configuration, loggerFactory);
            app.UseCloudFoundryActuator(configuration, loggerFactory);

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                app.UseThreadDumpActuator(configuration, loggerFactory);
                app.UseHeapDumpActuator(configuration, null, loggerFactory);
            }

            app.UseInfoActuator(configuration, loggerFactory);
            app.UseHealthActuator(new HealthOptions(configuration), new DefaultHealthAggregator(), healthContributors, loggerFactory);
            app.UseLoggersActuator(configuration, loggerProvider, loggerFactory);
            app.UseTraceActuator(configuration, null, loggerFactory);
            app.UseMappingActuator(configuration, apiExplorer, loggerFactory);
        }
    }
}
