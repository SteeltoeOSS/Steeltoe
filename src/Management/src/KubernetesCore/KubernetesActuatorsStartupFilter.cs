// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Steeltoe.Management.Endpoint;
using System;

namespace Steeltoe.Management.KubernetesCore
{
    public class KubernetesActuatorsStartupFilter : IStartupFilter
    {
        private MediaTypeVersion MediaTypeVersion { get; }

        public KubernetesActuatorsStartupFilter(MediaTypeVersion mediaTypeVersion)
        {
            MediaTypeVersion = mediaTypeVersion;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                next(app);
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapAllActuators(MediaTypeVersion);
                });
            };
        }
    }
}
