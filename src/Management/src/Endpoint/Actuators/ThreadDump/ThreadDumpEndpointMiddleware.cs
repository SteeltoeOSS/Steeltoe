// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.ThreadDump;

internal sealed class ThreadDumpEndpointMiddleware(
    IThreadDumpEndpointHandler endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor, ILoggerFactory loggerFactory)
    : EndpointMiddleware<object?, IList<ThreadInfo>>(endpointHandler, managementOptionsMonitor, loggerFactory)
{
    private const string PlainTextContentType = "text/plain";

    private protected override IReadOnlyList<string> ProducedContentTypes =>
    [
        ContentType,
        PlainTextContentType
    ];

    public override ActuatorMetadataProvider GetMetadataProvider()
    {
        return new ThreadDumpActuatorMetadataProvider(ContentType, PlainTextContentType);
    }

    protected override async Task<IList<ThreadInfo>> InvokeEndpointHandlerAsync(object? request, CancellationToken cancellationToken)
    {
        return await EndpointHandler.InvokeAsync(request, cancellationToken);
    }

    protected override async Task WriteResponseAsync(IList<ThreadInfo> response, HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (GetResponseContentType(httpContext.Request) == PlainTextContentType)
        {
            // Spring Boot Admin requests the plain text format when downloading a thread dump.
            httpContext.Response.ContentType = $"{PlainTextContentType}; charset=utf-8";
            await httpContext.Response.WriteAsync(ThreadDumpPlainTextFormatter.Format(response), cancellationToken);
        }
        else
        {
            await base.WriteResponseAsync(response, httpContext, cancellationToken);
        }
    }
}
