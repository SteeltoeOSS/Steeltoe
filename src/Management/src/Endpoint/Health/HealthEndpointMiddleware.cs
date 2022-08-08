// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint.Health;

public class HealthEndpointMiddleware : EndpointMiddleware<HealthEndpointResponse, ISecurityContext>
{
    private readonly RequestDelegate _next;

    public HealthEndpointMiddleware(RequestDelegate next, IManagementOptions managementOptions, ILogger<InfoEndpointMiddleware> logger = null)
        : base(managementOptions, logger)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context, HealthEndpointCore endpoint)
    {
        Endpoint = endpoint;

        if (Endpoint.ShouldInvoke(managementOptions))
        {
            return HandleHealthRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    protected internal Task HandleHealthRequestAsync(HttpContext context)
    {
        string serialInfo = DoRequest(context);
        logger?.LogDebug("Returning: {0}", serialInfo);

        context.HandleContentNegotiation(logger);
        return context.Response.WriteAsync(serialInfo);
    }

    protected internal string DoRequest(HttpContext context)
    {
        HealthEndpointResponse result = Endpoint.Invoke(new CoreSecurityContext(context));

        if (managementOptions.UseStatusCodeFromResponse)
        {
            context.Response.StatusCode = ((HealthEndpoint)Endpoint).GetStatusCode(result);
        }

        return Serialize(result);
    }
}
