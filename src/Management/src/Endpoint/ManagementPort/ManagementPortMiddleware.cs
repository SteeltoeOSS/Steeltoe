// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.ManagementPort;

internal sealed class ManagementPortMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<ManagementEndpointOptions> _managementOptionsMonitor;
    private readonly ILogger<ManagementPortMiddleware> _logger;

    public ManagementPortMiddleware(RequestDelegate next, IOptionsMonitor<ManagementEndpointOptions> managementOptionsMonitor,
        ILogger<ManagementPortMiddleware> logger)
    {
        ArgumentGuard.NotNull(logger);

        _next = next;
        _managementOptionsMonitor = managementOptionsMonitor;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentGuard.NotNull(context);
        ManagementEndpointOptions endpointOptions = _managementOptionsMonitor.CurrentValue;
        _logger.LogDebug("InvokeAsync({requestPath}), contextPath: {contextPath}", context.Request.Path.Value, endpointOptions.Path);

        string contextPath = endpointOptions.Path;
        bool isManagementPath = context.Request.Path.ToString().StartsWith(contextPath, StringComparison.OrdinalIgnoreCase);

        bool allowRequest = string.IsNullOrEmpty(endpointOptions.Port);
        allowRequest = allowRequest || (context.Request.Host.Port.ToString() == endpointOptions.Port && isManagementPath);
        allowRequest = allowRequest || (context.Request.Host.Port.ToString() != endpointOptions.Port && !isManagementPath);

        if (!allowRequest)
        {
            await ReturnErrorAsync(context, endpointOptions.Port);
        }
        else
        {
            await _next(context);
        }
    }

    private Task ReturnErrorAsync(HttpContext context, string managementPort)
    {
        var errorResponse = new ErrorResponse
        {
            Error = "Not Found",
            Message = "Path not found at port",
            Path = context.Request.Path,
            Status = StatusCodes.Status404NotFound
        };

        _logger.LogError("ManagementMiddleWare Error: Access denied on {port} since Management Port is set to {managementPort}", context.Request.Host.Port,
            managementPort);

        context.Response.Headers.Add("Content-Type", "application/json;charset=UTF-8");

        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return context.Response.WriteAsJsonAsync(errorResponse);
    }
}
