// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.Health;

namespace Steeltoe.Management.Endpoint;

internal sealed class ConfigureActuatorsMiddlewareStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder>? next)
    {
        return app =>
        {
            app.UseCors();

            app.UseManagementPort();

            if (app.ApplicationServices.GetService<ICloudFoundryEndpointHandler>() != null)
            {
                app.UseCloudFoundrySecurity();
            }

            next?.Invoke(app);

            app.UseActuators();

            app.ApplicationServices.InitializeAvailability();
        };
    }
}
