// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class TestConfigServerStartup
{
    public static string Host { get; set; }

    public static string Response { get; set; }

    public static int ReturnStatus { get; set; } = 200;

    public static HttpRequestInfo LastRequest { get; set; }

    public TestConfigServerStartup()
    {
        LastRequest = null;
    }

    public void Configure(IApplicationBuilder app)
    {
        app.Run(async context =>
        {
            if (!string.IsNullOrEmpty(Host) && Host != context.Request.Host.Value)
            {
                context.Response.StatusCode = 500;
                return;
            }

            LastRequest = await HttpRequestInfo.CreateAsync(context.Request);
            context.Response.StatusCode = ReturnStatus;
            context.Response.Headers.Add("content-type", "application/json");
            await context.Response.WriteAsync(Response);
        });
    }
}
