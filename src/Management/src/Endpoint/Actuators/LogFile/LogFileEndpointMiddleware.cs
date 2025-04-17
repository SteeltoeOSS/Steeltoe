// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.LogFile;

internal sealed class LogFileEndpointMiddleware(
    ILogFileEndpointHandler endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor, ILoggerFactory loggerFactory)
    : EndpointMiddleware<LogFileEndpointRequest?, LogFileEndpointResponse>(endpointHandler, managementOptionsMonitor, loggerFactory)
{
    protected override async Task<LogFileEndpointResponse> InvokeEndpointHandlerAsync(LogFileEndpointRequest? request, CancellationToken cancellationToken)
    {
        var logFileResponse = await EndpointHandler.InvokeAsync(request, cancellationToken);
        return logFileResponse;
    }

    protected override async Task<LogFileEndpointRequest?> ParseRequestAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        return httpContext.Request.Method.Equals(HttpMethod.Head.Method, StringComparison.OrdinalIgnoreCase)
            ? await Task.FromResult(new LogFileEndpointRequest(null, null, false))
            : null;
    }

    protected override async Task WriteResponseAsync(LogFileEndpointResponse response, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (response.LogFileEncoding != null)
        {
            httpContext.Response.ContentType = $"text/plain; charset={response.LogFileEncoding?.BodyName}";
        }
        else
        {
            httpContext.Response.ContentType = "text/plain;";
        }

        httpContext.Response.ContentLength = response.ContentLength;
        httpContext.Response.StatusCode = StatusCodes.Status200OK;

        if (httpContext.Request.Method.Equals(HttpMethod.Get.Method, StringComparison.OrdinalIgnoreCase))
        {

            if (response.Content != null)
            {
                await httpContext.Response.WriteAsync(response.Content, cancellationToken);
            }
        }
        else
        {
            httpContext.Response.Headers.AcceptRanges = "bytes";
            httpContext.Response.Headers.LastModified = response.LastModified?.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
