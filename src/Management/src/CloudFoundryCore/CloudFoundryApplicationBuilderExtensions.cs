// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
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
        ///// <summary>
        ///// Add all CloudFoundry Actuators (Info, Health, Loggers, Trace) and configure CORS
        ///// </summary>
        ///// <param name="app">AppBuilder needing actuators added</param>
        public static void UseCloudFoundryActuators(this IApplicationBuilder app)
        {
            app.UseCloudFoundryActuators(MediaTypeVersion.V1, ActuatorContext.CloudFoundry);
        }

        ///// <summary>
        ///// Add all CloudFoundry Actuators (Info, Health, Loggers, Trace) and configure CORS
        ///// </summary>
        ///// <param name="app">AppBuilder needing actuators added</param>
        ///// <param name="version">Mediatype version of the response</param>
        ///// <param name="context">Actuator context for endpoints</param>
        public static void UseCloudFoundryActuators(this IApplicationBuilder app, MediaTypeVersion version, ActuatorContext actuatorContext = ActuatorContext.ActuatorAndCloudFoundry)
        {
            if (actuatorContext != ActuatorContext.Actuator)
            {
                app.UseCors("SteeltoeManagement");
                app.UseCloudFoundrySecurity();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.Map<CloudFoundryEndpoint>();
                });
            }

            if (actuatorContext != ActuatorContext.CloudFoundry)
            {
                app.UseEndpoints(endpoints =>
                {
                    endpoints.Map<ActuatorEndpoint>();
                });
            }

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                app.UseEndpoints(endpoints =>
                {
                    endpoints.Map<ThreadDumpEndpoint_v2>();
                    endpoints.Map<HeapDumpEndpoint>();
                });
            }

            app.UseEndpoints(endpoints =>
            {
                endpoints.Map<InfoEndpoint>();
                endpoints.Map<HealthEndpoint>();
                endpoints.Map<LoggersEndpoint>();
                endpoints.Map<HttpTraceEndpoint>();
                endpoints.Map<MappingsEndpoint>();
            });
        }
    }
}
