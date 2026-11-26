// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.LogFile;

internal sealed class LogFileEndpointMiddleware(
    ILogFileEndpointHandler endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor, ILoggerFactory loggerFactory)
    : EndpointMiddleware<LogFileRequest, LogFileResponse>(endpointHandler, managementOptionsMonitor, loggerFactory)
{
    private const int BufferSize = 64 * 1024;

    private static readonly StringSegment BytesUnit = "bytes";
    private static readonly StringValues BytesUnitValues = BytesUnit.Value;

    // The encoding of the log file depends on the logging framework, so no charset is specified.
    private protected override string ContentType => "text/plain";

    protected override Task<LogFileRequest?> ParseRequestAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        RangeHeaderValue? rangeHeader = httpContext.Request.GetTypedHeaders().Range;

        if (rangeHeader == null || !StringSegment.Equals(rangeHeader.Unit, BytesUnit, StringComparison.OrdinalIgnoreCase))
        {
            // As permitted by RFC 9110, ignore a Range header that is absent, invalid, or uses an unknown unit.
            return Task.FromResult<LogFileRequest?>(new LogFileRequest());
        }

        return Task.FromResult<LogFileRequest?>(new LogFileRequest
        {
            Ranges = rangeHeader.Ranges.Select(range => new LogRange((ulong?)range.From, (ulong?)range.To)).ToList()
        });
    }

    protected override async Task<LogFileResponse> InvokeEndpointHandlerAsync(LogFileRequest? request, CancellationToken cancellationToken)
    {
        // ParseRequestAsync never returns null, so this indicates a bug.
        ArgumentNullException.ThrowIfNull(request);

        return await EndpointHandler.InvokeAsync(request, cancellationToken);
    }

    protected override async Task WriteResponseAsync(LogFileResponse result, HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(httpContext);

        using (result)
        {
            switch (result.Status)
            {
                case LogFileResponseStatus.Complete:
                case LogFileResponseStatus.Partial:
                {
                    await WriteContentAsync(result, httpContext, cancellationToken);
                    break;
                }
                case LogFileResponseStatus.RangeNotSatisfiable:
                {
                    httpContext.Response.Headers.AcceptRanges = BytesUnitValues;
                    httpContext.Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
                    httpContext.Response.GetTypedHeaders().ContentRange = new ContentRangeHeaderValue((long)result.TotalSize);
                    break;
                }
                default:
                {
                    httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                    break;
                }
            }
        }
    }

    private async Task WriteContentAsync(LogFileResponse result, HttpContext httpContext, CancellationToken cancellationToken)
    {
        httpContext.Response.Headers.AcceptRanges = BytesUnitValues;
        httpContext.Response.ContentType = ContentType;
        httpContext.Response.ContentLength = (long)result.Length;

        if (result.Status == LogFileResponseStatus.Partial)
        {
            httpContext.Response.StatusCode = StatusCodes.Status206PartialContent;

            httpContext.Response.GetTypedHeaders().ContentRange =
                new ContentRangeHeaderValue((long)result.Offset, (long)(result.Offset + result.Length - 1), (long)result.TotalSize);
        }
        else
        {
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
        }

        await CopyBytesAsync(result.Content!, httpContext.Response.Body, result.Length, cancellationToken);
    }

    private static async Task CopyBytesAsync(Stream source, Stream destination, ulong count, CancellationToken cancellationToken)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

        try
        {
            ulong remaining = count;

            while (remaining > 0)
            {
                int bytesToRead = (int)Math.Min((ulong)buffer.Length, remaining);
                int bytesRead = await source.ReadAsync(buffer.AsMemory(0, bytesToRead), cancellationToken);

                if (bytesRead == 0)
                {
                    // The file was truncated while being sent.
                    break;
                }

                await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                remaining -= (ulong)bytesRead;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
