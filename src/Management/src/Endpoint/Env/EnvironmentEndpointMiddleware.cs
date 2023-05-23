// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Env;

internal sealed class EnvironmentEndpointMiddleware : EndpointMiddleware<EnvironmentDescriptor>
{
    public EnvironmentEndpointMiddleware(IEnvironmentEndpoint endpointHandler, IOptionsMonitor<ManagementEndpointOptions> managementOptions, IOptionsMonitor<HttpMiddlewareOptions> endpointOptions, ILogger<EnvironmentEndpointMiddleware> logger) : base(endpointHandler, managementOptions, endpointOptions, logger)
    {
    }

    public override Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (EndpointOptions.CurrentValue.ShouldInvoke(ManagementOptions, context, Logger))
        {
            return HandleEnvRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    internal async Task HandleEnvRequestAsync(HttpContext context)
    {
        string serialInfo = await HandleRequestAsync(context.RequestAborted);
        Logger.LogDebug("Returning: {info}", serialInfo);

        context.HandleContentNegotiation(Logger);
        await context.Response.WriteAsync(serialInfo);
    }
}
