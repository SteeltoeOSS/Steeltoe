// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient.Test;

public class TestStartup
{
    public IConfiguration Configuration { get; set; }

    public TestStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.TryAddSingleton(new MyMiddleware());
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseMiddleware<MyMiddleware>();
    }
}
