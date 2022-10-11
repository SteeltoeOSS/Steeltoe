// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.ManagementPort;

public class ManagementPortMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ManagementPortMiddleware> _logger;
    private readonly IManagementOptions _managementOptions;

    public ManagementPortMiddleware(RequestDelegate next,  IEnumerable<IManagementOptions> managementOptions,
        ILogger<ManagementPortMiddleware> logger = null)
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
            await ReturnErrorAsync(context);
        }

        await _next(context);

    }

    


    private Task ReturnErrorAsync(HttpContext context)
    {
        context.Response.Headers.Add("Content-Type", "application/json;charset=UTF-8");

        // allowing override of 400-level errors is more likely to cause confusion than to be useful
        //if (_managementOptions.UseStatusCodeFromResponse || (int)error.Code < 500)
        //{
        //    context.Response.StatusCode = (int)error.Code;
        //}
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return context.Response.WriteAsync( "Forbidden");
    }

    //private void LogError(HttpContext context, SecurityResult error)
    //{
    //    _logger?.LogError("Actuator Security Error: {code} - {message}", error.Code, error.Message);

    //    if (_logger != null && _logger.IsEnabled(LogLevel.Trace))
    //    {
    //        foreach (KeyValuePair<string, StringValues> header in context.Request.Headers)
    //        {
    //            _logger.LogTrace("Header: {key} - {value}", header.Key, header.Value);
    //        }
    //    }
    //}
}
