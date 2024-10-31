// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO.Compression;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.HeapDump;

internal sealed class HeapDumpEndpointMiddleware(
    IHeapDumpEndpointHandler endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor, ILoggerFactory loggerFactory)
    : EndpointMiddleware<object?, string?>(endpointHandler, managementOptionsMonitor, loggerFactory)
{
    private readonly ILogger<HeapDumpEndpointMiddleware> _logger = loggerFactory.CreateLogger<HeapDumpEndpointMiddleware>();

    protected override async Task<string?> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        return await EndpointHandler.InvokeAsync(null, context.RequestAborted);
    }

    protected override async Task WriteResponseAsync(string? fileName, HttpContext context, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Returning: {FileName}", fileName);

        if (!File.Exists(fileName))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.ContentType = "application/octet-stream";
        context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{Path.GetFileName(fileName)}.gz\"");
        context.Response.StatusCode = StatusCodes.Status200OK;

        try
        {
            await using var inputStream = new FileStream(fileName, FileMode.Open);
            await using var outputStream = new GZipStream(context.Response.Body, CompressionLevel.Fastest, true);
            await inputStream.CopyToAsync(outputStream, cancellationToken);
        }
        finally
        {
            File.Delete(fileName);
        }
    }
}
