// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint;

internal sealed class MapActuatorsStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder>? next)
    {
        return app =>
        {
            if (app.ApplicationServices.GetService<ICorsService>() != null)
            {
                app.UseCors(ActuatorsCorsPolicyOptions.PolicyName);
            }

            if (app.ApplicationServices.GetService<PermissionsProvider>() != null)
            {
                app.UseCloudFoundrySecurity();
            }

            app.UseManagementPort();

            next?.Invoke(app);

            var mvcOptions = app.ApplicationServices.GetService<IOptions<MvcOptions>>();
            bool isEndpointRoutingEnabled = mvcOptions?.Value.EnableEndpointRouting ?? true;

            if (isEndpointRoutingEnabled)
            {
                app.UseActuators();
            }
            else
            {
                app.UseMvc(routeBuilder => routeBuilder.MapActuators());
            }

            app.ApplicationServices.InitializeAvailability();
        };
    }
}
