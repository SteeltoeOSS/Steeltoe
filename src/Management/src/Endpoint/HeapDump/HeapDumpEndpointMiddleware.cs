// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.HeapDump;

public class HeapDumpEndpointMiddleware : EndpointMiddleware<string>
{
    private readonly RequestDelegate _next;

    public HeapDumpEndpointMiddleware(RequestDelegate next, HeapDumpEndpoint endpoint, IManagementOptions managementOptions,
        ILogger<HeapDumpEndpointMiddleware> logger = null)
        : base(endpoint, managementOptions, logger)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        if (Endpoint.ShouldInvoke(managementOptions, logger))
        {
            return HandleHeapDumpRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    protected internal async Task HandleHeapDumpRequestAsync(HttpContext context)
    {
        string fileName = Endpoint.Invoke();
        logger?.LogDebug("Returning: {fileName}", fileName);
        context.Response.Headers.Add("Content-Type", "application/octet-stream");

        if (!File.Exists(fileName))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        string gzFileName = $"{fileName}.gz";
        Stream result = await Utils.CompressFileAsync(fileName, gzFileName);

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
