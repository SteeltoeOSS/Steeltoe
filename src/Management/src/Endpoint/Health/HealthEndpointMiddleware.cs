// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint.Health;

internal sealed class HealthEndpointMiddleware : EndpointMiddleware<HealthEndpointResponse, ISecurityContext>
{
    public HealthEndpointMiddleware(IOptionsMonitor<ManagementEndpointOptions> managementOptions, IHealthEndpoint endpoint,
        ILogger<HealthEndpointMiddleware> logger)
        : base(managementOptions, logger)
    {
        Endpoint = endpoint;
    }

    public override Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (Endpoint.Options.ShouldInvoke(ManagementOptions, context, Logger))
        {
            return HandleHealthRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    internal async Task HandleHealthRequestAsync(HttpContext context)
    {
        string serialInfo = await DoRequestAsync(context);
        Logger.LogDebug("Returning: {info}", serialInfo);

        context.HandleContentNegotiation(Logger);
        await context.Response.WriteAsync(serialInfo);
    }

    internal async Task<string> DoRequestAsync(HttpContext context)
    {
        HealthEndpointResponse result = await ((HealthEndpoint)Endpoint).InvokeAsync(context.RequestAborted, new CoreSecurityContext(context));

        ManagementEndpointOptions currentOptions = ManagementOptions.CurrentValue;

        if (currentOptions.UseStatusCodeFromResponse)
        {
            context.Response.StatusCode = ((HealthEndpoint)Endpoint).GetStatusCode(result);
        }

        return Serialize(result);
    }
}
