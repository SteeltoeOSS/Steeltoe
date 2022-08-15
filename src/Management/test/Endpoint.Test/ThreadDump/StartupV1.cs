// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.CloudFoundry;

namespace Steeltoe.Management.Endpoint.ThreadDump.Test;

public class StartupV1
{
    public IConfiguration Configuration { get; set; }

    public StartupV1(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddCloudFoundryActuator(Configuration);
        services.AddThreadDumpActuator(Configuration, MediaTypeVersion.V1);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.Map<ThreadDumpEndpoint>();
        });
    }
}
