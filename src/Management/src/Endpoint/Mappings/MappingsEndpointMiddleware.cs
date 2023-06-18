// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Mappings;

public class MappingsEndpointMiddleware : EndpointMiddleware<ApplicationMappings>
{
    public MappingsEndpointMiddleware(IOptionsMonitor<MappingsEndpointOptions> options, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        IMappingsEndpoint endpoint, ILogger<MappingsEndpointMiddleware> logger)
        : base(endpoint, managementOptions, logger)
    {
    }

    public override Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (Endpoint.Options.ShouldInvoke(managementOptions, context, logger))
        {
            return HandleMappingsRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    protected internal Task HandleMappingsRequestAsync(HttpContext context)
    {
        ApplicationMappings result = Endpoint.Invoke();
        string serialInfo = Serialize(result);

        logger.LogDebug("Returning: {info}", serialInfo);

        context.HandleContentNegotiation(logger);
        return context.Response.WriteAsync(serialInfo);
    }
}
