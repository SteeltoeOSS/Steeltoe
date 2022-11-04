// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Health;

namespace Steeltoe.Management.Endpoint.Test.Health;

public class AuthStartup
{
    public IConfiguration Configuration { get; }

    public AuthStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddHealthActuator(Configuration);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseMiddleware<AuthenticatedTestMiddleware>();
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.Map<HealthEndpointCore>();
        });
    }
}
