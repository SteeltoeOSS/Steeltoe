// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.HeapDump;

internal sealed class HeapDumpEndpointMiddleware : EndpointMiddleware<string>
{
    public HeapDumpEndpointMiddleware(IHeapDumpEndpoint endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        ILogger<HeapDumpEndpointMiddleware> logger)
        : base(endpoint, managementOptions, logger)
    {
        Endpoint = endpoint;
    }

    public override Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (Endpoint.Options.ShouldInvoke(ManagementOptions, context, Logger))
        {
            return HandleHeapDumpRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    internal async Task HandleHeapDumpRequestAsync(HttpContext context)
    {
        string fileName = await Endpoint.InvokeAsync(context.RequestAborted);
        Logger.LogDebug("Returning: {fileName}", fileName);
        context.Response.Headers.Add("Content-Type", "application/octet-stream");

        if (!File.Exists(fileName))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        string gzFileName = $"{fileName}.gz";
        Stream result = await Utils.CompressFileAsync(fileName, gzFileName, Logger);

        if (result != null)
        {
            await using (result)
            {
                context.Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{Path.GetFileName(gzFileName)}\"");
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentLength = result.Length;
                await result.CopyToAsync(context.Response.Body);
            }

            File.Delete(gzFileName);
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
        }
    }
}
