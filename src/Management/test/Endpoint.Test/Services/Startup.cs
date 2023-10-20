// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Web.Hypermedia;
using Steeltoe.Management.Endpoint.Services;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.Test.Services;

public class Startup
{
    public IConfiguration Configuration { get; set; }

    // Test Activator Utils Constructor works 
    [ActivatorUtilitiesConstructor]
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }
    public Startup(IConfiguration configuration, ILogger<Startup> logger): this(configuration)
    {

    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddHypermediaActuator();
        services.AddServicesActuator();
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
