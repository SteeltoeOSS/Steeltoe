// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test;

public class TestConfigServerStartup
{
    public static CountdownEvent InitialRequestLatch = new(1);

    public static string Response { get; set; }

    public static int[] ReturnStatus { get; set; } =
    {
        200
    };

    public static HttpRequestInfo LastRequest { get; set; }

    public static int RequestCount { get; set; }

    public static string Label { get; set; } = string.Empty;

    public static string AppName { get; set; } = string.Empty;

    public static string Env { get; set; } = string.Empty;

    public static void Reset()
    {
        Response = null;

        ReturnStatus = new[]
        {
            200
        };

        LastRequest = null;
        RequestCount = 0;
        Label = AppName = Env = string.Empty;
        InitialRequestLatch = new CountdownEvent(1);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.Run(async context =>
        {
            LastRequest = new HttpRequestInfo(context.Request);
            context.Response.StatusCode = GetStatusCode(context.Request.Path);
            RequestCount++;

            if (context.Response.StatusCode == 200)
            {
                context.Response.Headers.Add("content-type", "application/json");
                await context.Response.WriteAsync(Response);
            }

            if (RequestCount == 1)
            {
                InitialRequestLatch.Signal();
            }
        });
    }

    public int GetStatusCode(string path)
    {
        if (!string.IsNullOrEmpty(Label) && !path.Contains(Label))
        {
            return 404;
        }

        if (!string.IsNullOrEmpty(Env) && !path.Contains(Env))
        {
            return 404;
        }

        if (!string.IsNullOrEmpty(AppName) && !path.Contains(AppName))
        {
            return 404;
        }

        return ReturnStatus[RequestCount];
    }
}
