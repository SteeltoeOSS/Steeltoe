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
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly RequestDelegate? _next;
    private readonly ILogger<ManagementPortMiddleware> _logger;

    public ManagementPortMiddleware(IOptionsMonitor<ManagementOptions> managementOptionsMonitor, RequestDelegate? next,
        ILogger<ManagementPortMiddleware> logger)
    {
        ArgumentGuard.NotNull(managementOptionsMonitor);
        ArgumentGuard.NotNull(logger);

        _managementOptionsMonitor = managementOptionsMonitor;
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentGuard.NotNull(context);

        ManagementOptions managementOptions = _managementOptionsMonitor.CurrentValue;
        _logger.LogDebug("InvokeAsync({requestPath}), optionsPath: {optionsPath}", context.Request.Path.Value, managementOptions.Path);

        bool isManagementPath = context.Request.Path.StartsWithSegments(managementOptions.Path);

        bool allowRequest = string.IsNullOrEmpty(managementOptions.Port);
        allowRequest = allowRequest || (context.Request.Host.Port.ToString() == managementOptions.Port && isManagementPath);
        allowRequest = allowRequest || (context.Request.Host.Port.ToString() != managementOptions.Port && !isManagementPath);

        if (!allowRequest)
        {
            await ReturnErrorAsync(context, managementOptions.Port);
        }
        else
        {
            if (_next != null)
            {
                await _next(context);
            }
        }
    }

    private Task ReturnErrorAsync(HttpContext context, string? managementPort)
    {
        var errorResponse = new ErrorResponse(DateTime.UtcNow, StatusCodes.Status404NotFound, "Not Found", "Path not found at port", context.Request.Path);

        _logger.LogError("ManagementMiddleWare Error: Access denied on {port} since Management Port is set to {managementPort}", context.Request.Host.Port,
            managementPort);

        context.Response.Headers.Append("Content-Type", "application/json;charset=UTF-8");
        context.Response.StatusCode = StatusCodes.Status404NotFound;

        return context.Response.WriteAsJsonAsync(errorResponse);
    }
}
