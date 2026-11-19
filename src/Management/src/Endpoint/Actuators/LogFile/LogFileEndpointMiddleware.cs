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
    : EndpointMiddleware<LogFileRequest, LogFileResult>(endpointHandler, managementOptionsMonitor, loggerFactory)
{
    private const string BytesUnit = "bytes";
    private const int BufferSize = 64 * 1024;

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

        if (rangeHeader.Ranges.Count != 1)
        {
            return Task.FromResult<LogFileRequest?>(new LogFileRequest
            {
                HasMultipleRanges = true
            });
        }

        RangeItemHeaderValue rangeItem = rangeHeader.Ranges.Single();

        return Task.FromResult<LogFileRequest?>(new LogFileRequest
        {
            RangeStart = rangeItem.From,
            RangeEnd = rangeItem.To
        });
    }

    protected override async Task<LogFileResult> InvokeEndpointHandlerAsync(LogFileRequest? request, CancellationToken cancellationToken)
    {
        return await EndpointHandler.InvokeAsync(request ?? new LogFileRequest(), cancellationToken);
    }

    protected override async Task WriteResponseAsync(LogFileResult result, HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(httpContext);

        using (result)
        {
            switch (result.Status)
            {
                case LogFileResultStatus.Complete:
                case LogFileResultStatus.Partial:
                {
                    await WriteContentAsync(result, httpContext, cancellationToken);
                    break;
                }
                case LogFileResultStatus.RangeNotSatisfiable:
                {
                    httpContext.Response.Headers.AcceptRanges = BytesUnit;
                    httpContext.Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
                    httpContext.Response.GetTypedHeaders().ContentRange = new ContentRangeHeaderValue(result.TotalLength);
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

    private async Task WriteContentAsync(LogFileResult result, HttpContext httpContext, CancellationToken cancellationToken)
    {
        httpContext.Response.Headers.AcceptRanges = BytesUnit;
        httpContext.Response.ContentType = ContentType;
        httpContext.Response.ContentLength = result.Length;

        if (result.Status == LogFileResultStatus.Partial)
        {
            httpContext.Response.StatusCode = StatusCodes.Status206PartialContent;

            httpContext.Response.GetTypedHeaders().ContentRange =
                new ContentRangeHeaderValue(result.Offset, result.Offset + result.Length - 1, result.TotalLength);
        }
        else
        {
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
        }

        await CopyBytesAsync(result.Content!, httpContext.Response.Body, result.Length, cancellationToken);
    }

    private static async Task CopyBytesAsync(Stream source, Stream destination, long count, CancellationToken cancellationToken)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

        try
        {
            long remaining = count;

            while (remaining > 0)
            {
                int bytesRead = await source.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), cancellationToken);

                if (bytesRead == 0)
                {
                    // The file was truncated while being sent.
                    break;
                }

                await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                remaining -= bytesRead;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
