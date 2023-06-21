// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.HeapDump;

internal sealed class HeapDumpEndpointMiddleware : EndpointMiddleware<object, string>
{
    private readonly ILogger<HeapDumpEndpointMiddleware> _logger;
    public HeapDumpEndpointMiddleware(IHeapDumpEndpointHandler endpointHandler, IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILoggerFactory loggerFactory) : base(endpointHandler, managementOptions, loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<HeapDumpEndpointMiddleware>();
    }

    protected override async Task<string> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        string fileName = await EndpointHandler.InvokeAsync(null, context.RequestAborted);
        _logger.LogDebug("Returning: {fileName}", fileName);
        context.Response.Headers["Content-Type"] = "application/octet-stream";

        if (!File.Exists(fileName))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return string.Empty;
        }

        string gzFileName = $"{fileName}.gz";
        Stream result = await Utils.CompressFileAsync(fileName, gzFileName, _logger);

        if (result != null)
        {
            try
            {
                await using (result)
                {
                    context.Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{Path.GetFileName(gzFileName)}\"");
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentLength = result.Length;
                    await result.CopyToAsync(context.Response.Body);
                }
            }
            finally
            {
                File.Delete(gzFileName);
            }
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
        }

        return string.Empty;
    }
}
