// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Health;
using System;

namespace Steeltoe.Management.Kubernetes;

[Obsolete("This class will be removed in a future release, Use Steeltoe.Management.Endpoint.AllActuatorsStartupFilter instead")]
public class KubernetesActuatorsStartupFilter : IStartupFilter
{
    private readonly MediaTypeVersion _mediaTypeVersion;

    public KubernetesActuatorsStartupFilter(MediaTypeVersion mediaTypeVersion)
    {
        _mediaTypeVersion = mediaTypeVersion;
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            next(app);
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapAllActuators(_mediaTypeVersion);
            });
            app.ApplicationServices.InitializeAvailability();
        };
    }
}