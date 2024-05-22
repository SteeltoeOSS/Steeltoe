// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text.Json;
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
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly IOptionsMonitor<CloudFoundryEndpointOptions> _endpointOptionsMonitor;
    private readonly ICollection<EndpointOptions> _endpointOptionsCollection;
    private readonly RequestDelegate? _next;
    private readonly ILogger<CloudFoundrySecurityMiddleware> _logger;
    private readonly SecurityUtils _securityUtils;

    public CloudFoundrySecurityMiddleware(IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IOptionsMonitor<CloudFoundryEndpointOptions> endpointOptionsMonitor, IEnumerable<EndpointOptions> endpointOptionsCollection, RequestDelegate? next,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(managementOptionsMonitor);
        ArgumentGuard.NotNull(endpointOptionsMonitor);
        ArgumentGuard.NotNull(endpointOptionsCollection);
        ArgumentGuard.NotNull(loggerFactory);

        _managementOptionsMonitor = managementOptionsMonitor;
        _endpointOptionsMonitor = endpointOptionsMonitor;

        _endpointOptionsCollection = endpointOptionsCollection.Where(options => options is not HypermediaEndpointOptions).ToList();

        _next = next;
        _logger = loggerFactory.CreateLogger<CloudFoundrySecurityMiddleware>();
        _securityUtils = new SecurityUtils(endpointOptionsMonitor.CurrentValue, loggerFactory.CreateLogger<SecurityUtils>());
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentGuard.NotNull(context);

        _logger.LogDebug("InvokeAsync({RequestPath})", context.Request.Path.Value);
        CloudFoundryEndpointOptions endpointOptions = _endpointOptionsMonitor.CurrentValue;

        if (Platform.IsCloudFoundry && endpointOptions.IsEnabled(_managementOptionsMonitor.CurrentValue) &&
            SecurityUtils.IsCloudFoundryRequest(context.Request.Path))
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

            EndpointOptions? targetEndpointOptions = FindTargetEndpoint(context.Request.Path);

            if (targetEndpointOptions == null)
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

            if (targetEndpointOptions.RequiredPermissions > givenPermissions.Permissions)
            {
                await ReturnErrorAsync(context, new SecurityResult(HttpStatusCode.Forbidden, SecurityUtils.AccessDeniedMessage));
                return;
            }
        }

        if (_next != null)
        {
            await _next(context);
        }
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

        return string.Empty;
    }

    internal Task<SecurityResult> GetPermissionsAsync(HttpContext context)
    {
        string accessToken = GetAccessToken(context.Request);
        return _securityUtils.GetPermissionsAsync(accessToken, context.RequestAborted);
    }

    private EndpointOptions? FindTargetEndpoint(PathString requestPath)
    {
        foreach (EndpointOptions endpointOptions in _endpointOptionsCollection)
        {
            string basePath = ConfigureManagementOptions.DefaultCloudFoundryPath;

            if (!string.IsNullOrEmpty(endpointOptions.Path))
            {
                basePath += '/';
            }

            if (endpointOptions is CloudFoundryEndpointOptions)
            {
                if (requestPath.Equals(basePath))
                {
                    return endpointOptions;
                }
            }
            else
            {
                if (requestPath.StartsWithSegments(basePath + endpointOptions.Path))
                {
                    return endpointOptions;
                }
            }
        }

        return null;
    }

    private async Task ReturnErrorAsync(HttpContext context, SecurityResult error)
    {
        LogError(context, error);
        context.Response.Headers.Append("Content-Type", "application/json;charset=UTF-8");

        // allowing override of 400-level errors is more likely to cause confusion than to be useful
        if (_managementOptionsMonitor.CurrentValue.UseStatusCodeFromResponse || (int)error.Code < 500)
        {
            context.Response.StatusCode = (int)error.Code;
        }

        await JsonSerializer.SerializeAsync(context.Response.Body, error, cancellationToken: context.RequestAborted);
    }

    private void LogError(HttpContext context, SecurityResult error)
    {
        _logger.LogError("Actuator Security Error: {Code} - {Message}", error.Code, error.Message);

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            foreach (KeyValuePair<string, StringValues> header in context.Request.Headers)
            {
                _logger.LogTrace("Header: {Key} - {Value}", header.Key, header.Value);
            }
        }
    }
}
