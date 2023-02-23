// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Env;

public class EnvEndpointMiddleware : EndpointMiddleware<EnvironmentDescriptor>, IEndpointMiddleware
{
    public EnvEndpointMiddleware(/*RequestDelegate next, */EnvEndpoint endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILogger<EnvEndpointMiddleware> logger = null)
        : base(endpoint, managementOptions, logger)
    {
        Endpoint = endpoint;
    }
    
    public EnvEndpoint EnvEndpoint { get; }

    public IEndpointOptions EndpointOptions => throw new NotImplementedException();

    public Task InvokeAsync(HttpContext context)
    {
        if (EnvEndpoint.Options.CurrentValue.EndpointOptions.ShouldInvoke(managementOptions.CurrentValue, logger))
        {
            return HandleEnvRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    protected internal Task HandleEnvRequestAsync(HttpContext context)
    {
        string serialInfo = HandleRequest();
        logger?.LogDebug("Returning: {info}", serialInfo);

        context.HandleContentNegotiation(logger);
        return context.Response.WriteAsync(serialInfo);
    }
}
