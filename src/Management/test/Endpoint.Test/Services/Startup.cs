// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Services;
using Steeltoe.Management.Endpoint.Web.Hypermedia;

namespace Steeltoe.Management.Endpoint.Test.Services;

public sealed class Startup
{
    [ActivatorUtilitiesConstructor]
    public Startup(IConfiguration configuration)
    {
        _ = configuration;
    }

    public Startup(IConfiguration configuration, ILogger<Startup> logger)
        : this(configuration)
    {
        _ = logger;
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
        app.UseEndpoints(endpoints => endpoints.MapAllActuators());
    }
}
