// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointCore.Test.SpringBootAdminClient
{
    public class TestStartup
    {
        public TestStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.TryAddSingleton(new MyMiddleware());
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMiddleware<MyMiddleware>();
        }
    }

#pragma warning disable SA1402 // File may only contain a single type
    public class MyMiddleware : IMiddleware
#pragma warning restore SA1402 // File may only contain a single type
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Path.Value.EndsWith("instances"))
            {
                // Registration response
                await context.Response.WriteAsync("{\"Id\":\"1234567\"}");
            }
            else
            {
                // Unregister response
                await context.Response.WriteAsync("Ok!");
            }
        }
    }
}
