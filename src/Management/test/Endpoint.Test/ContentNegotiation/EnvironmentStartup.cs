// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Actuators.Environment;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;

namespace Steeltoe.Management.Endpoint.Test.ContentNegotiation;

public sealed class EnvironmentStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHypermediaActuator();
        services.AddEnvironmentActuator();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapAllActuators();
        });
    }
}
