// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Info;

public class InfoEndpointMiddleware : EndpointMiddleware<Dictionary<string, object>>
{
    public InfoEndpointMiddleware(IInfoEndpoint endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILogger<InfoEndpointMiddleware> logger)
        : base(endpoint, managementOptions, logger)
    {
    }

    public override Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentGuard.NotNull(context);
        ArgumentGuard.NotNull(next);

        Logger.LogDebug("Info middleware InvokeAsync({path})", context.Request.Path.Value);

        if (Endpoint.Options.ShouldInvoke(ManagementOptions, context, Logger))
        {
            return HandleInfoRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    internal Task HandleInfoRequestAsync(HttpContext context)
    {
        string serialInfo = HandleRequest();
        Logger.LogDebug("Returning: {info}", serialInfo);

        context.HandleContentNegotiation(Logger);
        return context.Response.WriteAsync(serialInfo);
    }
}
