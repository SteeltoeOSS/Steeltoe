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

internal sealed class MappingsEndpointMiddleware : EndpointMiddleware<object, ApplicationMappings>
{
    public MappingsEndpointMiddleware(IOptionsMonitor<MappingsEndpointOptions> options, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        IOptionsMonitor<HttpMiddlewareOptions> endpointOptions,
        IMappingsEndpointHandler endpointHandler, ILogger<MappingsEndpointMiddleware> logger)
        : base(endpointHandler, managementOptions, endpointOptions, logger)
    {
    }

    public override Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (EndpointOptions.CurrentValue.ShouldInvoke(ManagementOptions, context, Logger))
        {
            return HandleMappingsRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    public override bool ShouldInvoke(HttpContext context)
    {
        throw new NotImplementedException();
    }

    protected override async Task<ApplicationMappings> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        return await EndpointHandler.InvokeAsync(null, context.RequestAborted);
    }

    internal async Task HandleMappingsRequestAsync(HttpContext context)
    {
        //ApplicationMappings result = await Endpoint.InvokeAsync(context.RequestAborted);
        //string serialInfo = Serialize(result);

        //Logger.LogDebug("Returning: {info}", serialInfo);

        //context.HandleContentNegotiation(Logger);
        //await context.Response.WriteAsync(serialInfo);
    }
}
