// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Health;

namespace Steeltoe.Management.CloudFoundry;

[Obsolete("This class will be removed in a future release, Use Steeltoe.Management.Endpoint.AllActuatorsStartupFilter instead")]
public class CloudFoundryActuatorsStartupFilter : IStartupFilter
{
    public CloudFoundryActuatorsStartupFilter()
    {
    }

    [Obsolete("MediaTypeVersion parameter is not used")]
    public CloudFoundryActuatorsStartupFilter(MediaTypeVersion mediaTypeVersion)
    {
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseCors("SteeltoeManagement");
            app.UseCloudFoundrySecurity();

            next(app);

            app.UseEndpoints(endpoints => endpoints.MapAllActuators(null));
            app.ApplicationServices.InitializeAvailability();
        };
    }
}
