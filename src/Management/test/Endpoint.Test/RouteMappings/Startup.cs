// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.RouteMappings;

namespace Steeltoe.Management.Endpoint.Test.RouteMappings;

public sealed class Startup
{
    private readonly IConfiguration _configuration;

    private bool UseEndpointRouting => _configuration.GetValue("TestUsesEndpointRouting", true);

    public Startup(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCloudFoundryActuator();
        services.AddMappingsActuator();

        services.AddMvc(options =>
        {
            if (!UseEndpointRouting)
            {
                options.EnableEndpointRouting = false;
            }
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        if (UseEndpointRouting)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapAllActuators();
            });
        }
        else
        {
            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
                routes.MapAllActuators();

                routes.AddRoutesToMappingsActuator();
            });
        }
    }
}
