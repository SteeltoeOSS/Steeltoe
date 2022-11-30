// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.ManagementPort;

public class ManagementPortMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ManagementPortMiddleware> _logger;
    private readonly IManagementOptions _managementOptions;

    public ManagementPortMiddleware(RequestDelegate next, IEnumerable<IManagementOptions> managementOptions, ILogger<ManagementPortMiddleware> logger = null)
    {
        _next = next;
        _logger = logger;
        _managementOptions = managementOptions.OfType<ManagementEndpointOptions>().First();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger?.LogDebug("InvokeAsync({requestPath}), contextPath: {contextPath}", context.Request.Path.Value, _managementOptions.Path);

        string contextPath = _managementOptions.Path;
        bool isManagementPath = context.Request.Path.ToString().StartsWith(contextPath, StringComparison.OrdinalIgnoreCase);

        bool allowRequest = string.IsNullOrEmpty(_managementOptions.Port);
        allowRequest = allowRequest || (context.Request.Host.Port.ToString() == _managementOptions.Port && isManagementPath);
        allowRequest = allowRequest || (context.Request.Host.Port.ToString() != _managementOptions.Port && !isManagementPath);

        if (!allowRequest)
        {
            await ReturnErrorAsync(context, _managementOptions.Port);
        }
        else
        {
            await _next(context);
        }
    }

    private Task ReturnErrorAsync(HttpContext context, string managementPort)
    {
        string errorMessage = $"Access denied to {context.Request.Path} on port {context.Request.Host.Port} since Management Port is set to {managementPort} ";
        _logger?.LogError("ManagementMiddleWare Error: {0}", errorMessage);
        context.Response.Headers.Add("Content-Type", "application/json;charset=UTF-8");

        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return context.Response.WriteAsync(errorMessage);
    }
}
