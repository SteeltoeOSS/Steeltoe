// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Hypermedia;
using System;

namespace Steeltoe.Management.CloudFoundry
{
    public class CloudFoundryActuatorsStartupFilter : IStartupFilter
    {
        private ActuatorContext ActuatorContext { get; }

        private MediaTypeVersion MediaTypeVersion { get; }

        public CloudFoundryActuatorsStartupFilter(MediaTypeVersion mediaTypeVersion, ActuatorContext actuatorContext)
        {
            MediaTypeVersion = mediaTypeVersion;
            ActuatorContext = actuatorContext;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.UseCloudFoundryActuators(MediaTypeVersion, ActuatorContext);

                next(app);
            };
        }
    }
}
