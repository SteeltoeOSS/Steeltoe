// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

public class CloudFoundrySecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CloudFoundrySecurityMiddleware> _logger;
    private readonly ICloudFoundryOptions _options;
    private readonly IManagementOptions _managementOptions;
    private readonly SecurityBase _base;

    public CloudFoundrySecurityMiddleware(RequestDelegate next, ICloudFoundryOptions options, CloudFoundryManagementOptions managementOptions, ILogger<CloudFoundrySecurityMiddleware> logger = null)
    {
        _next = next;
        _logger = logger;
        _options = options;
        _managementOptions = managementOptions;

        _base = new SecurityBase(options, _managementOptions, logger);
    }

    public async Task Invoke(HttpContext context)
    {
        _logger?.LogDebug("Invoke({0}) contextPath: {1}", context.Request.Path.Value, _managementOptions.Path);

        var isEndpointExposed = _managementOptions == null || _options.IsExposed(_managementOptions);

        if (Platform.IsCloudFoundry
            && isEndpointExposed
            && _base.IsCloudFoundryRequest(context.Request.Path))
        {
            if (string.IsNullOrEmpty(_options.ApplicationId))
            {
                _logger?.LogCritical("The Application Id could not be found. Make sure the Cloud Foundry Configuration Provider has been added to the application configuration.");
                await ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, _base.ApplicationIdMissingMessage)).ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrEmpty(_options.CloudFoundryApi))
            {
                await ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, _base.CloudfoundryApiMissingMessage)).ConfigureAwait(false);
                return;
            }

            var target = FindTargetEndpoint(context.Request.Path);
            if (target == null)
            {
                await ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, _base.EndpointNotConfiguredMessage)).ConfigureAwait(false);
                return;
            }

            var sr = await GetPermissions(context).ConfigureAwait(false);
            if (sr.Code != HttpStatusCode.OK)
            {
                await ReturnError(context, sr).ConfigureAwait(false);
                return;
            }

            var permissions = sr.Permissions;
            if (!target.IsAccessAllowed(permissions))
            {
                await ReturnError(context, new SecurityResult(HttpStatusCode.Forbidden, _base.AccessDeniedMessage)).ConfigureAwait(false);
                return;
            }
        }

        await _next(context).ConfigureAwait(false);
    }

    internal string GetAccessToken(HttpRequest request)
    {
        if (request.Headers.TryGetValue(_base.AuthorizationHeader, out var headerVal))
        {
            var header = headerVal.ToString();
            if (header.StartsWith(_base.Bearer, StringComparison.OrdinalIgnoreCase))
            {
                return header.Substring(_base.Bearer.Length + 1);
            }
        }

        return null;
    }

    internal Task<SecurityResult> GetPermissions(HttpContext context)
    {
        var token = GetAccessToken(context.Request);
        return _base.GetPermissionsAsync(token);
    }

    private IEndpointOptions FindTargetEndpoint(PathString path)
    {
        List<IEndpointOptions> configEndpoints;

        configEndpoints = _managementOptions.EndpointOptions;
        foreach (var ep in configEndpoints)
        {
            var contextPath = _managementOptions.Path;
            if (!contextPath.EndsWith("/") && !string.IsNullOrEmpty(ep.Path))
            {
                contextPath += "/";
            }

            var fullPath = contextPath + ep.Path;

            if (ep is CloudFoundryEndpointOptions)
            {
                if (path.Value.Equals(contextPath, StringComparison.OrdinalIgnoreCase))
                {
                    return ep;
                }
            }
            else if (path.StartsWithSegments(new PathString(fullPath)))
            {
                return ep;
            }
        }

        return null;
    }

    private Task ReturnError(HttpContext context, SecurityResult error)
    {
        LogError(context, error);
        context.Response.Headers.Add("Content-Type", "application/json;charset=UTF-8");

        // allowing override of 400-level errors is more likely to cause confusion than to be useful
        if (_managementOptions.UseStatusCodeFromResponse || (int)error.Code < 500)
        {
            context.Response.StatusCode = (int)error.Code;
        }

        return context.Response.WriteAsync(_base.Serialize(error));
    }

    private void LogError(HttpContext context, SecurityResult error)
    {
        _logger?.LogError("Actuator Security Error: {0} - {1}", error.Code, error.Message);
        if (_logger != null && _logger.IsEnabled(LogLevel.Trace))
        {
            foreach (var header in context.Request.Headers)
            {
                _logger.LogTrace("Header: {0} - {1}", header.Key, header.Value);
            }
        }
    }
}
