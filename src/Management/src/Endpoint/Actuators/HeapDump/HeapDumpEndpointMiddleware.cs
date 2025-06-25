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
    : EndpointMiddleware<object?, string>(endpointHandler, managementOptionsMonitor, loggerFactory)
{
    private readonly ILogger<HeapDumpEndpointMiddleware> _logger = loggerFactory.CreateLogger<HeapDumpEndpointMiddleware>();

    private protected override string ContentType => "application/octet-stream";

    protected override async Task<string> InvokeEndpointHandlerAsync(object? request, CancellationToken cancellationToken)
    {
        return await EndpointHandler.InvokeAsync(request, cancellationToken);
    }

    protected override async Task WriteResponseAsync(string? fileName, HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        _logger.LogDebug("Returning: {FileName}", fileName);

        if (!File.Exists(fileName))
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        httpContext.Response.ContentType = ContentType;
        httpContext.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{Path.GetFileName(fileName)}.gz\"");
        httpContext.Response.StatusCode = StatusCodes.Status200OK;

        try
        {
            await using var inputStream = new FileStream(fileName, FileMode.Open);
            await using var outputStream = new GZipStream(httpContext.Response.Body, CompressionLevel.Fastest, true);
            await inputStream.CopyToAsync(outputStream, cancellationToken);
        }
        finally
        {
            File.Delete(fileName);
        }
    }
}
