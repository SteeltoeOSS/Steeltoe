// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.Health;

internal sealed class HealthEndpointMiddleware : EndpointMiddleware<HealthEndpointRequest, HealthEndpointResponse>
{
    private readonly IOptionsMonitor<HealthEndpointOptions> _endpointOptionsMonitor;
    private readonly ILogger<HealthEndpointMiddleware> _logger;

    public HealthEndpointMiddleware(IHealthEndpointHandler endpointHandler, IOptionsMonitor<HealthEndpointOptions> endpointOptionsMonitor,
        IOptionsMonitor<ManagementOptions> managementOptionsMonitor, ILoggerFactory loggerFactory)
        : base(endpointHandler, managementOptionsMonitor, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(endpointOptionsMonitor);

        _endpointOptionsMonitor = endpointOptionsMonitor;
        _logger = loggerFactory.CreateLogger<HealthEndpointMiddleware>();
    }

    protected override async Task<HealthEndpointResponse> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        HealthEndpointOptions currentEndpointOptions = _endpointOptionsMonitor.CurrentValue;
        string groupName = GetRequestedHealthGroup(context.Request.Path, currentEndpointOptions, _logger);

        if (!IsValidGroup(groupName, currentEndpointOptions))
        {
            return new HealthEndpointResponse
            {
                Exists = false
            };
        }

        bool hasClaim = GetHasClaim(context, currentEndpointOptions);

        var request = new HealthEndpointRequest(groupName, hasClaim);
        return await EndpointHandler.InvokeAsync(request, context.RequestAborted);
    }

    /// <summary>
    /// Returns the last segment of the HTTP request path, which is expected to be the name of a configured health group.
    /// </summary>
    private static string GetRequestedHealthGroup(PathString requestPath, HealthEndpointOptions endpointOptions, ILogger<HealthEndpointMiddleware> logger)
    {
        string[] requestComponents = requestPath.Value?.Split('/') ?? [];

        if (requestComponents.Length > 0 && requestComponents[^1] != endpointOptions.Id)
        {
            logger.LogTrace("Found group '{HealthGroup}' in the request path.", requestComponents[^1]);
            return requestComponents[^1];
        }

        logger.LogTrace("Did not find a health group in the request path.");

        return string.Empty;
    }

    private static bool IsValidGroup(string groupName, HealthEndpointOptions endpointOptions)
    {
        return string.IsNullOrEmpty(groupName) || endpointOptions.Groups.Any(pair => pair.Key == groupName);
    }

    private static bool GetHasClaim(HttpContext context, HealthEndpointOptions endpointOptions)
    {
        EndpointClaim? claim = endpointOptions.Claim;
        return claim is { Type: not null, Value: not null } && context.User.HasClaim(claim.Type, claim.Value);
    }

    protected override async Task WriteResponseAsync(HealthEndpointResponse result, HttpContext context, CancellationToken cancellationToken)
    {
        if (!result.Exists)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        if (ManagementOptionsMonitor.CurrentValue.UseStatusCodeFromResponse || UseStatusCodeFromResponseInHeader(context.Request.Headers))
        {
            context.Response.StatusCode = ((HealthEndpointHandler)EndpointHandler).GetStatusCode(result);
        }

        await base.WriteResponseAsync(result, context, cancellationToken);
    }

    private static bool UseStatusCodeFromResponseInHeader(IHeaderDictionary requestHeaders)
    {
        if (requestHeaders.TryGetValue("X-Use-Status-Code-From-Response", out StringValues headerValue) && bool.TryParse(headerValue, out bool useStatusCode))
        {
            return useStatusCode;
        }

        return false;
    }
}
