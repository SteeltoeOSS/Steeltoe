// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint.Health;

internal sealed class HealthEndpointMiddleware : EndpointMiddleware<HealthEndpointRequest, HealthEndpointResponse>
{
    private readonly IOptionsMonitor<HealthEndpointOptions> _endpointOptionsMonitor;
    private readonly ILogger<HealthEndpointMiddleware> _logger;

    public HealthEndpointMiddleware(IHealthEndpointHandler endpointHandler, IOptionsMonitor<HealthEndpointOptions> endpointOptionsMonitor,
        IOptionsMonitor<ManagementOptions> managementOptionsMonitor, ILoggerFactory loggerFactory)
        : base(endpointHandler, managementOptionsMonitor, loggerFactory)
    {
        ArgumentGuard.NotNull(endpointOptionsMonitor);

        _endpointOptionsMonitor = endpointOptionsMonitor;
        _logger = loggerFactory.CreateLogger<HealthEndpointMiddleware>();
    }

    protected override async Task<HealthEndpointResponse> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        string groupName = GetRequestedHealthGroup(context.Request.Path);

        if (!IsValidGroup(groupName))
        {
            return new HealthEndpointResponse
            {
                Exists = false
            };
        }

        bool hasClaim = GetHasClaim(context);

        var request = new HealthEndpointRequest(groupName, hasClaim);
        return await EndpointHandler.InvokeAsync(request, context.RequestAborted);
    }

    /// <summary>
    /// Returns the last segment of the HTTP request path, which is expected to be the name of a configured health group.
    /// </summary>
    private string GetRequestedHealthGroup(PathString requestPath)
    {
        string[] requestComponents = requestPath.Value?.Split('/') ?? Array.Empty<string>();

        if (requestComponents.Length > 0)
        {
            return requestComponents[^1];
        }

        _logger.LogWarning("Failed to find anything in the request from which to parse health group name.");

        return string.Empty;
    }

    private bool IsValidGroup(string groupName)
    {
        return groupName == _endpointOptionsMonitor.CurrentValue.Id || groupName == "{**_}" ||
            _endpointOptionsMonitor.CurrentValue.Groups.Any(pair => pair.Key == groupName);
    }

    private bool GetHasClaim(HttpContext context)
    {
        EndpointClaim? claim = _endpointOptionsMonitor.CurrentValue.Claim;
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
