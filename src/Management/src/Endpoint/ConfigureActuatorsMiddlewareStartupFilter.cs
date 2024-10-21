// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;

namespace Steeltoe.Management.Endpoint;

internal sealed class ConfigureActuatorsMiddlewareStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder>? next)
    {
        return app =>
        {
            // According to https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing, apps typically don't need to call UseRouting; if not
            // explicitly called, UseRouting is implicitly inserted as the first middleware to execute.
            // However, UseActuatorEndpoints currently fails without an explicit call to UseRouting, and
            // https://learn.microsoft.com/en-us/aspnet/core/security/cors states that UseCors must be placed after UseRouting.
            // The ordering used here allows for next() to call UseAuthentication/UseAuthorization, which must be placed between UseRouting and UseActuatorEndpoints.

            app.UseManagementPort();

            if (app.ApplicationServices.GetService<ICloudFoundryEndpointHandler>() != null)
            {
                app.UseCloudFoundrySecurity();
            }

            app.UseRouting();
            app.UseActuatorsCorsPolicy();

            next?.Invoke(app);

            app.UseActuatorEndpoints();
        };
    }
}
