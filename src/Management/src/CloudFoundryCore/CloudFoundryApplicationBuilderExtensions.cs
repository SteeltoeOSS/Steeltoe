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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.Hypermedia;
using System;

namespace Steeltoe.Management.CloudFoundry
{
    public static class CloudFoundryApplicationBuilderExtensions
    {
        /// <summary>
        /// Add all CloudFoundry Actuators (Info, Health, Loggers, Trace) and configure CORS
        /// </summary>
        /// <param name="app">AppBuilder needing actuators added</param>
        public static void UseCloudFoundryActuators(this IApplicationBuilder app)
        {
            app.UseCloudFoundryActuators(MediaTypeVersion.V1, ActuatorContext.CloudFoundry);
        }

        /// <summary>
        /// Add all CloudFoundry Actuators (Info, Health, Loggers, Trace) and configure CORS
        /// </summary>
        /// <param name="app">AppBuilder needing actuators added</param>
        /// <param name="version">Mediatype version of the response</param>
        /// <param name="context">Actuator context for endpoints</param>
        /// <param name="buildCorsPolicy">Customize the CORS policy. </param>
        public static void UseCloudFoundryActuators(this IApplicationBuilder app, MediaTypeVersion version, ActuatorContext context, Action<CorsPolicyBuilder> buildCorsPolicy = null)
        {
            if (context != ActuatorContext.Actuator)
            {
                app.UseCors(builder =>
                {
                    builder
                        .WithMethods("GET", "POST")
                        .WithHeaders("Authorization", "X-Cf-App-Instance", "Content-Type");

                    if (buildCorsPolicy != null)
                    {
                        buildCorsPolicy(builder);
                    }
                    else
                    {
                        builder.AllowAnyOrigin();
                    }
                });

                app.UseCloudFoundrySecurity();
                app.UseCloudFoundryActuator();
            }

            if (context != ActuatorContext.CloudFoundry)
            {
                app.UseHypermediaActuator();
            }

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                app.UseThreadDumpActuator(version);
                app.UseHeapDumpActuator();
            }

            app.UseInfoActuator();
            app.UseHealthActuator();
            app.UseLoggersActuator();
            app.UseTraceActuator(version);
            app.UseMappingsActuator();
        }
    }
}
