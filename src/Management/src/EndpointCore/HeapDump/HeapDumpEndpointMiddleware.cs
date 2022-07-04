// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Middleware;
using System.IO;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.HeapDump;

public class HeapDumpEndpointMiddleware : EndpointMiddleware<string>
{
    private readonly RequestDelegate _next;

    public HeapDumpEndpointMiddleware(RequestDelegate next, HeapDumpEndpoint endpoint, IManagementOptions mgmtOptions, ILogger<HeapDumpEndpointMiddleware> logger = null)
        : base(endpoint, mgmtOptions, logger: logger)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        if (innerEndpoint.ShouldInvoke(mgmtOptions, logger))
        {
            return HandleHeapDumpRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    protected internal async Task HandleHeapDumpRequestAsync(HttpContext context)
    {
        var filename = innerEndpoint.Invoke();
        logger?.LogDebug("Returning: {0}", filename);
        context.Response.Headers.Add("Content-Type", "application/octet-stream");

        if (!File.Exists(filename))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var gzFilename = $"{filename}.gz";
        var result = await Utils.CompressFileAsync(filename, gzFilename).ConfigureAwait(false);

        if (result != null)
        {
            await using (result)
            {
                context.Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{Path.GetFileName(gzFilename)}\"");
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentLength = result.Length;
                await result.CopyToAsync(context.Response.Body).ConfigureAwait(false);
            }

            File.Delete(gzFilename);
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
        }
    }
}
