// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Web.Hypermedia;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

public sealed class CloudFoundrySecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CloudFoundrySecurityMiddleware> _logger;
    private readonly IOptionsMonitor<CloudFoundryEndpointOptions> _options;

    private readonly IOptionsMonitor<ManagementEndpointOptions> _managementOptionsMonitor;
    private readonly IEnumerable<HttpMiddlewareOptions> _endpointsCollection;
    private readonly SecurityUtils _base;

    public CloudFoundrySecurityMiddleware(RequestDelegate next, IOptionsMonitor<CloudFoundryEndpointOptions> options,
        IOptionsMonitor<ManagementEndpointOptions> managementOptionsMonitor, IEnumerable<HttpMiddlewareOptions> endpointsCollection,
        ILogger<CloudFoundrySecurityMiddleware> logger)
    {
        ArgumentGuard.NotNull(logger);
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(managementOptionsMonitor);
        ArgumentGuard.NotNull(endpointsCollection);

        _next = next;
        _logger = logger;
        _options = options;
        _managementOptionsMonitor = managementOptionsMonitor;
        _endpointsCollection = endpointsCollection.Where(ep => ep is not HypermediaEndpointOptions && ep is not CloudFoundryEndpointOptions);

        _base = new SecurityUtils(_options.CurrentValue, logger);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentGuard.NotNull(context);
        string contextPath = ConfigureManagementEndpointOptions.DefaultCFPath;
        CloudFoundryEndpointOptions endpointOptions = _options.CurrentValue;
        _logger.LogDebug("InvokeAsync({requestPath}), contextPath: {contextPath}", context.Request.Path.Value, contextPath);

        if (Platform.IsCloudFoundry && endpointOptions.IsEnabled(_managementOptionsMonitor.CurrentValue) && _base.IsCloudFoundryRequest(context.Request.Path))
        {
            if (string.IsNullOrEmpty(endpointOptions.ApplicationId))
            {
                _logger.LogCritical(
                    "The Application Id could not be found. Make sure the Cloud Foundry Configuration Provider has been added to the application configuration.");

                await ReturnErrorAsync(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, SecurityUtils.ApplicationIdMissingMessage));

                return;
            }

            if (string.IsNullOrEmpty(endpointOptions.CloudFoundryApi))
            {
                await ReturnErrorAsync(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, SecurityUtils.CloudfoundryApiMissingMessage));

                return;
            }

            HttpMiddlewareOptions target = FindTargetEndpoint(context.Request.Path);

            if (target == null)
            {
                await ReturnErrorAsync(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, SecurityUtils.EndpointNotConfiguredMessage));

                return;
            }

            SecurityResult givenPermissions = await GetPermissionsAsync(context);

            if (givenPermissions.Code != HttpStatusCode.OK)
            {
                await ReturnErrorAsync(context, givenPermissions);
                return;
            }

            if (target.RequiredPermissions > givenPermissions.Permissions)
            {
                await ReturnErrorAsync(context, new SecurityResult(HttpStatusCode.Forbidden, SecurityUtils.AccessDeniedMessage));
                return;
            }
        }

        await _next(context);
    }

    internal string GetAccessToken(HttpRequest request)
    {
        if (request.Headers.TryGetValue(SecurityUtils.AuthorizationHeader, out StringValues headerVal))
        {
            string header = headerVal.ToString();

            if (header.StartsWith(SecurityUtils.Bearer, StringComparison.OrdinalIgnoreCase))
            {
                return header.Substring(SecurityUtils.Bearer.Length + 1);
            }
        }

        return null;
    }

    internal Task<SecurityResult> GetPermissionsAsync(HttpContext context)
    {
        string token = GetAccessToken(context.Request);
        return _base.GetPermissionsAsync(token);
    }

    private HttpMiddlewareOptions FindTargetEndpoint(PathString path)
    {
        foreach (HttpMiddlewareOptions ep in _endpointsCollection)
        {
            string contextPath = ConfigureManagementEndpointOptions.DefaultCFPath;

            if (!contextPath.EndsWith('/') && !string.IsNullOrEmpty(ep.Path))
            {
                contextPath += '/';
            }

            string fullPath = contextPath + ep.Path;

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

    private Task ReturnErrorAsync(HttpContext context, SecurityResult error)
    {
        LogError(context, error);
        context.Response.Headers.Add("Content-Type", "application/json;charset=UTF-8");

        // allowing override of 400-level errors is more likely to cause confusion than to be useful
        if (_managementOptionsMonitor.CurrentValue.UseStatusCodeFromResponse || (int)error.Code < 500)
        {
            context.Response.StatusCode = (int)error.Code;
        }

        return context.Response.WriteAsync(_base.Serialize(error));
    }

    private void LogError(HttpContext context, SecurityResult error)
    {
        _logger.LogError("Actuator Security Error: {code} - {message}", error.Code, error.Message);

        if (_logger != null && _logger.IsEnabled(LogLevel.Trace))
        {
            foreach (KeyValuePair<string, StringValues> header in context.Request.Headers)
            {
                _logger.LogTrace("Header: {key} - {value}", header.Key, header.Value);
            }
        }
    }
}
