// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.ThreadDump;

internal sealed class ThreadDumpEndpointMiddleware : EndpointMiddleware<object, IList<ThreadInfo>>
{
    private readonly ILogger<ThreadDumpEndpointMiddleware> _logger;
    internal MediaTypeVersion MediaTypeVersion { get; set; }

    public ThreadDumpEndpointMiddleware(IThreadDumpEndpointHandler endpointHandler, IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILoggerFactory loggerFactory)
        : base(endpointHandler, managementOptions, loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ThreadDumpEndpointMiddleware>();
    }

    protected override async Task<IList<ThreadInfo>> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Executing ThreadDumpHandler");

        return await EndpointHandler.InvokeAsync(null, cancellationToken);
    }
    protected override JsonSerializerOptions GetSerializerOptions()
    {
        JsonSerializerOptions serializerOptions = base.GetSerializerOptions();
        if (MediaTypeVersion == MediaTypeVersion.V2 && serializerOptions.Converters?.Any(c => c is ThreadDumpV2Converter) != true)
        {
            _logger.LogDebug("Adding ThreadDumpV2Converter");
            serializerOptions.Converters.Add(new ThreadDumpV2Converter());
        }
        return serializerOptions;
    }
}
