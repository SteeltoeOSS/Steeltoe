// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint.Health;

public class HealthEndpointMiddleware : EndpointMiddleware<HealthEndpointResponse, ISecurityContext>, IMiddleware
{
    

    public HealthEndpointMiddleware(/*RequestDelegate next,*/ IOptionsMonitor<ManagementEndpointOptions> managementOptions,IHealthEndpoint endpoint, ILogger<InfoEndpointMiddleware> logger = null)
        : base(managementOptions, logger)
    {
        Endpoint = (IEndpoint<HealthEndpointResponse, ISecurityContext>)endpoint;
    }
    public Task InvokeAsync(HttpContext context , RequestDelegate next)
    {

        if (Endpoint.Options.ShouldInvoke(managementOptions, context, logger))
        {
            return HandleHealthRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    protected internal Task HandleHealthRequestAsync(HttpContext context)
    {
        string serialInfo = DoRequest(context);
        logger?.LogDebug("Returning: {info}", serialInfo);

        context.HandleContentNegotiation(logger);
        return context.Response.WriteAsync(serialInfo);
    }
    
    protected internal string DoRequest(HttpContext context)
    {
        HealthEndpointResponse result = ((HealthEndpointCore)Endpoint).Invoke(new CoreSecurityContext(context));

        var currentOptions = managementOptions.CurrentValue;
        if (currentOptions.UseStatusCodeFromResponse)
        {
            context.Response.StatusCode = ((HealthEndpointCore)Endpoint).GetStatusCode(result);
        }

        return Serialize(result);
    }
}
