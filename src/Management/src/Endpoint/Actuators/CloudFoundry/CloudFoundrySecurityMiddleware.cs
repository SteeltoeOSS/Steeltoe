// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Steeltoe.Common;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.CloudFoundry;

internal sealed class CloudFoundrySecurityMiddleware
{
    private const string BearerTokenPrefix = "Bearer ";
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly IOptionsMonitor<CloudFoundryEndpointOptions> _endpointOptionsMonitor;
    private readonly IEndpointOptionsMonitorProvider[] _endpointOptionsMonitorProviderArray;
    private readonly RequestDelegate? _next;
    private readonly ILogger<CloudFoundrySecurityMiddleware> _logger;
    private readonly PermissionsProvider _permissionsProvider;

    public CloudFoundrySecurityMiddleware(IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IOptionsMonitor<CloudFoundryEndpointOptions> endpointOptionsMonitor, IEnumerable<IEndpointOptionsMonitorProvider> endpointOptionsMonitorProviders,
        PermissionsProvider permissionsProvider, ILogger<CloudFoundrySecurityMiddleware> logger, RequestDelegate? next)
    {
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);
        ArgumentNullException.ThrowIfNull(endpointOptionsMonitor);
        ArgumentNullException.ThrowIfNull(endpointOptionsMonitorProviders);
        ArgumentNullException.ThrowIfNull(permissionsProvider);
        ArgumentNullException.ThrowIfNull(logger);

        IEndpointOptionsMonitorProvider[] endpointOptionsMonitorProviderArray = endpointOptionsMonitorProviders.ToArray();
        ArgumentGuard.ElementsNotNull(endpointOptionsMonitorProviderArray);

        _managementOptionsMonitor = managementOptionsMonitor;
        _endpointOptionsMonitor = endpointOptionsMonitor;
        _endpointOptionsMonitorProviderArray = endpointOptionsMonitorProviderArray;
        _permissionsProvider = permissionsProvider;
        _logger = logger;
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogDebug("InvokeAsync({RequestPath})", context.Request.Path.Value);
        CloudFoundryEndpointOptions endpointOptions = _endpointOptionsMonitor.CurrentValue;
        ManagementOptions managementOptions = _managementOptionsMonitor.CurrentValue;

        if (Platform.IsCloudFoundry && endpointOptions.IsEnabled(managementOptions) && managementOptions.IsCloudFoundryEnabled &&
            PermissionsProvider.IsCloudFoundryRequest(context.Request.Path))
        {
            if (string.IsNullOrEmpty(endpointOptions.ApplicationId))
            {
                _logger.LogError(
                    "The Application Id could not be found. Make sure the Cloud Foundry Configuration Provider has been added to the application configuration.");

                await ReturnErrorAsync(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, PermissionsProvider.Messages.ApplicationIdMissing));
                return;
            }

            if (string.IsNullOrEmpty(endpointOptions.Api))
            {
                await ReturnErrorAsync(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, PermissionsProvider.Messages.CloudFoundryApiMissing));
                return;
            }

            EndpointOptions? targetEndpointOptions = FindTargetEndpoint(context.Request.Path);

            if (targetEndpointOptions != null)
            {
                SecurityResult givenPermissions = await GetPermissionsAsync(context);

                if (givenPermissions.Code != HttpStatusCode.OK)
                {
                    await ReturnErrorAsync(context, givenPermissions);
                    return;
                }

                if (targetEndpointOptions.RequiredPermissions > givenPermissions.Permissions)
                {
                    await ReturnErrorAsync(context, new SecurityResult(HttpStatusCode.Forbidden, PermissionsProvider.Messages.AccessDenied));
                    return;
                }
            }
        }

        if (_next != null)
        {
            await _next(context);
        }
    }

    internal string GetAccessToken(HttpRequest request)
    {
        if (request.Headers.TryGetValue(HeaderNames.Authorization, out StringValues authorizationHeaderValue))
        {
            string authorizationValue = authorizationHeaderValue.ToString();

            if (authorizationValue.StartsWith(BearerTokenPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return authorizationValue[BearerTokenPrefix.Length..];
            }
        }

        return string.Empty;
    }

    internal async Task<SecurityResult> GetPermissionsAsync(HttpContext context)
    {
        string accessToken = GetAccessToken(context.Request);
        SecurityResult permissionsResult = await _permissionsProvider.GetPermissionsAsync(accessToken, context.RequestAborted);

        if (permissionsResult.Permissions != EndpointPermissions.None)
        {
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken? jwtSecurityToken = jwtSecurityTokenHandler.ReadJwtToken(accessToken);
            var claimsIdentity = new ClaimsIdentity();
            claimsIdentity.AddClaims(jwtSecurityToken.Claims);
            context.User.AddIdentity(claimsIdentity);
        }

        return permissionsResult;
    }

    private EndpointOptions? FindTargetEndpoint(PathString requestPath)
    {
        foreach (EndpointOptions endpointOptions in _endpointOptionsMonitorProviderArray.Select(provider => provider.Get())
            .Where(options => options is not HypermediaEndpointOptions))
        {
            string endpointPath = endpointOptions.GetEndpointPath(ConfigureManagementOptions.DefaultCloudFoundryPath);

            if (endpointOptions is CloudFoundryEndpointOptions)
            {
                if (requestPath == endpointPath)
                {
                    return endpointOptions;
                }
            }
            else
            {
                if (requestPath.StartsWithSegments(endpointPath))
                {
                    return endpointOptions;
                }
            }
        }

        return null;
    }

    private async Task ReturnErrorAsync(HttpContext context, SecurityResult error)
    {
        _logger.LogError("Actuator Security Error: {Code} - {Message}", error.Code, error.Message);
        context.Response.Headers.Append("Content-Type", "application/json;charset=UTF-8");

        // UseStatusCodeFromResponse was added to prevent IIS/HWC from blocking the response body on 500-level errors.
        // Blocking 400-level error responses would be more likely to cause confusion than to be useful.
        // See https://github.com/SteeltoeOSS/Steeltoe/issues/418 for more information.
        if (_managementOptionsMonitor.CurrentValue.UseStatusCodeFromResponse || (int)error.Code < 500)
        {
            context.Response.StatusCode = (int)error.Code;
        }

        await JsonSerializer.SerializeAsync(context.Response.Body, error, cancellationToken: context.RequestAborted);
    }
}
