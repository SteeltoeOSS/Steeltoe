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
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.CloudFoundry;

internal sealed class CloudFoundrySecurityMiddleware
{
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly IOptionsMonitor<CloudFoundryEndpointOptions> _endpointOptionsMonitor;
    private readonly ICollection<EndpointOptions> _endpointOptionsCollection;
    private readonly RequestDelegate? _next;
    private readonly ILogger<CloudFoundrySecurityMiddleware> _logger;
    private readonly PermissionsProvider _permissionsProvider;

    public CloudFoundrySecurityMiddleware(IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IOptionsMonitor<CloudFoundryEndpointOptions> endpointOptionsMonitor, IEnumerable<EndpointOptions> endpointOptionsCollection,
        PermissionsProvider permissionsProvider, ILogger<CloudFoundrySecurityMiddleware> logger, RequestDelegate? next)
    {
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);
        ArgumentNullException.ThrowIfNull(endpointOptionsMonitor);
        ArgumentNullException.ThrowIfNull(endpointOptionsCollection);
        ArgumentNullException.ThrowIfNull(permissionsProvider);
        ArgumentNullException.ThrowIfNull(logger);

        EndpointOptions[] endpointOptionsArray = endpointOptionsCollection.ToArray();
        ArgumentGuard.ElementsNotNull(endpointOptionsArray);

        _managementOptionsMonitor = managementOptionsMonitor;
        _endpointOptionsMonitor = endpointOptionsMonitor;
        _endpointOptionsCollection = endpointOptionsArray.Where(options => options is not HypermediaEndpointOptions).ToArray();
        _permissionsProvider = permissionsProvider;
        _logger = logger;
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogDebug("InvokeAsync({RequestPath})", context.Request.Path.Value);
        CloudFoundryEndpointOptions endpointOptions = _endpointOptionsMonitor.CurrentValue;

        if (Platform.IsCloudFoundry && endpointOptions.IsEnabled(_managementOptionsMonitor.CurrentValue) &&
            PermissionsProvider.IsCloudFoundryRequest(context.Request.Path))
        {
            if (string.IsNullOrEmpty(endpointOptions.ApplicationId))
            {
                _logger.LogCritical(
                    "The Application Id could not be found. Make sure the Cloud Foundry Configuration Provider has been added to the application configuration.");

                await ReturnErrorAsync(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, PermissionsProvider.ApplicationIdMissingMessage));

                return;
            }

            if (string.IsNullOrEmpty(endpointOptions.Api))
            {
                await ReturnErrorAsync(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, PermissionsProvider.CloudfoundryApiMissingMessage));

                return;
            }

            EndpointOptions? targetEndpointOptions = FindTargetEndpoint(context.Request.Path);

            if (targetEndpointOptions == null)
            {
                await ReturnErrorAsync(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, PermissionsProvider.EndpointNotConfiguredMessage));

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
                await ReturnErrorAsync(context, new SecurityResult(HttpStatusCode.Forbidden, PermissionsProvider.AccessDeniedMessage));
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
        if (request.Headers.TryGetValue(PermissionsProvider.AuthorizationHeaderName, out StringValues headerVal))
        {
            string header = headerVal.ToString();

            if (header.StartsWith(PermissionsProvider.BearerHeaderNamePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return header[PermissionsProvider.BearerHeaderNamePrefix.Length..];
            }
        }

        return string.Empty;
    }

    internal Task<SecurityResult> GetPermissionsAsync(HttpContext context)
    {
        string accessToken = GetAccessToken(context.Request);
        return _permissionsProvider.GetPermissionsAsync(accessToken, context.RequestAborted);
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
