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

internal sealed partial class LogFileEndpointMiddleware(
    ILogFileEndpointHandler endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor, ILoggerFactory loggerFactory)
    : EndpointMiddleware<object?, string?>(endpointHandler, managementOptionsMonitor, loggerFactory)
{
    private const string BytesUnit = "bytes";
    private const int BufferSize = 64 * 1024;

    private readonly ILogger<LogFileEndpointMiddleware> _logger = loggerFactory.CreateLogger<LogFileEndpointMiddleware>();

    // The encoding of the log file depends on the logging framework, so no charset is specified.
    private protected override string ContentType => "text/plain";

    protected override async Task<string?> InvokeEndpointHandlerAsync(object? request, CancellationToken cancellationToken)
    {
        return await EndpointHandler.InvokeAsync(request, cancellationToken);
    }

    protected override async Task WriteResponseAsync(string? filePath, HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (filePath == null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        FileStream? fileStream = OpenFile(filePath);

        if (fileStream == null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await using (fileStream)
        {
            // The logger may append to the file while it is being sent. Only the bytes that exist now are sent, so the response matches its headers.
            long fileLength = fileStream.Length;
            httpContext.Response.Headers.AcceptRanges = BytesUnit;

            if (!TryGetByteRange(httpContext.Request, fileLength, out ByteRange? range))
            {
                LogRangeNotSatisfiable(httpContext.Request.Headers.Range, filePath, fileLength);
                httpContext.Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
                httpContext.Response.GetTypedHeaders().ContentRange = new ContentRangeHeaderValue(fileLength);
                return;
            }

            long offset = 0;
            long length = fileLength;

            if (range is { } byteRange)
            {
                offset = byteRange.Start;
                length = byteRange.Length;

                httpContext.Response.StatusCode = StatusCodes.Status206PartialContent;
                httpContext.Response.GetTypedHeaders().ContentRange = new ContentRangeHeaderValue(byteRange.Start, byteRange.End, fileLength);
            }
            else
            {
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
            }

            httpContext.Response.ContentType = ContentType;
            httpContext.Response.ContentLength = length;

            LogReturning(length, offset, filePath);

            fileStream.Seek(offset, SeekOrigin.Begin);
            await CopyBytesAsync(fileStream, httpContext.Response.Body, length, cancellationToken);
        }
    }

    private FileStream? OpenFile(string filePath)
    {
        try
        {
            // Share access with the logger, so it can continue to write, roll over, or delete the file while it is being read.
            return new FileStream(filePath, new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.ReadWrite | FileShare.Delete,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                BufferSize = 0
            });
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            // Some logging frameworks don't create the file until the first entry is written, so this is expected.
            LogFileNotFound(filePath);
            return null;
        }
    }

    /// <summary>
    /// Determines which bytes to send, based on the Range request header.
    /// </summary>
    /// <param name="request">
    /// The incoming HTTP request.
    /// </param>
    /// <param name="fileLength">
    /// The current length of the file, in bytes.
    /// </param>
    /// <param name="range">
    /// The range of bytes to send, or <c>null</c> to send the entire file.
    /// </param>
    /// <returns>
    /// <c>false</c> if the requested range can't be satisfied; otherwise <c>true</c>.
    /// </returns>
    private static bool TryGetByteRange(HttpRequest request, long fileLength, out ByteRange? range)
    {
        range = null;
        RangeHeaderValue? rangeHeader = request.GetTypedHeaders().Range;

        if (rangeHeader == null || !StringSegment.Equals(rangeHeader.Unit, BytesUnit, StringComparison.OrdinalIgnoreCase))
        {
            // As permitted by RFC 9110, ignore a Range header that is absent, invalid, or uses an unknown unit.
            return true;
        }

        if (rangeHeader.Ranges.Count != 1)
        {
            // Multipart responses are not supported. Rejecting the request is cheaper than sending the entire file, which can be large.
            return false;
        }

        RangeItemHeaderValue rangeItem = rangeHeader.Ranges.Single();

        if (rangeItem.From == null)
        {
            // Suffix range, such as "bytes=-500", which requests the last 500 bytes.
            long suffixLength = rangeItem.To!.Value;

            if (suffixLength == 0 || fileLength == 0)
            {
                return false;
            }

            range = new ByteRange(Math.Max(fileLength - suffixLength, 0), fileLength - 1);
            return true;
        }

        if (rangeItem.From.Value >= fileLength)
        {
            return false;
        }

        long end = rangeItem.To == null ? fileLength - 1 : Math.Min(rangeItem.To.Value, fileLength - 1);
        range = new ByteRange(rangeItem.From.Value, end);
        return true;
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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Log file '{FilePath}' does not exist.")]
    private partial void LogFileNotFound(string filePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Range '{Range}' is not satisfiable for log file '{FilePath}' with length {FileLength}.")]
    private partial void LogRangeNotSatisfiable(StringValues range, string filePath, long fileLength);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Returning {Length} bytes at offset {Offset} from log file '{FilePath}'.")]
    private partial void LogReturning(long length, long offset, string filePath);

    private readonly record struct ByteRange(long Start, long End)
    {
        public long Length => End - Start + 1;
    }
}
