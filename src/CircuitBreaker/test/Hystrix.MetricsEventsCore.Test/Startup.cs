// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Controllers;
using System.Reflection;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Test;

public class Startup
{
    public IConfiguration Configuration { get; set; }

    public Startup()
    {
        var builder = new ConfigurationBuilder();
        Configuration = builder.Build();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHystrixMonitoringStreams(Configuration);
        var metricsAssembly = typeof(HystrixStreamBaseController).GetTypeInfo().Assembly;
        var s = services
            .AddMvc().ConfigureApplicationPartManager(apm =>
            {
                apm.ApplicationParts.Add(new AssemblyPart(metricsAssembly));
            });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseHystrixRequestContext();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        });
    }
}