// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common.HealthChecks;
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

    public override ActuatorMetadataProvider GetMetadataProvider()
    {
        return new HealthActuatorMetadataProvider(ContentType);
    }

    protected override Task<HealthEndpointRequest?> ParseRequestAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        HealthEndpointRequest? request = null;
        HealthEndpointOptions options = _endpointOptionsMonitor.CurrentValue;
        string groupName = GetRequestedHealthGroup(httpContext.Request.Path, options);

        if (IsValidGroup(groupName, options))
        {
            bool hasClaim = GetHasClaim(httpContext, options);
            request = new HealthEndpointRequest(groupName, hasClaim);
        }

        return Task.FromResult(request);
    }

    /// <summary>
    /// Returns the last segment of the HTTP request path, which is expected to be the name of a configured health group.
    /// </summary>
    private string GetRequestedHealthGroup(PathString requestPath, HealthEndpointOptions endpointOptions)
    {
        string[] requestComponents = requestPath.Value?.Split('/') ?? [];

        if (requestComponents.Length > 0 && requestComponents[^1] != endpointOptions.Id)
        {
            _logger.LogTrace("Found group '{HealthGroup}' in the request path.", requestComponents[^1]);
            return requestComponents[^1];
        }

        _logger.LogTrace("Did not find a health group in the request path.");
        return string.Empty;
    }

    private static bool IsValidGroup(string groupName, HealthEndpointOptions endpointOptions)
    {
        return groupName.Length == 0 || endpointOptions.Groups.ContainsKey(groupName);
    }

    private static bool GetHasClaim(HttpContext context, HealthEndpointOptions endpointOptions)
    {
        EndpointClaim? claim = endpointOptions.Claim;
        return claim is { Type: not null, Value: not null } && context.User.HasClaim(claim.Type, claim.Value);
    }

    protected override async Task<HealthEndpointResponse> InvokeEndpointHandlerAsync(HealthEndpointRequest? request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return new HealthEndpointResponse
            {
                Exists = false
            };
        }

        return await EndpointHandler.InvokeAsync(request, cancellationToken);
    }

    protected override async Task WriteResponseAsync(HealthEndpointResponse response, HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(httpContext);

        if (!response.Exists)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        bool? headerValue = UseStatusCodeFromResponseInHeader(httpContext.Request.Headers);

        if (headerValue ?? ManagementOptionsMonitor.CurrentValue.UseStatusCodeFromResponse)
        {
            httpContext.Response.StatusCode = GetStatusCode(response);
        }

        await base.WriteResponseAsync(response, httpContext, cancellationToken);
    }

    private static bool? UseStatusCodeFromResponseInHeader(IHeaderDictionary requestHeaders)
    {
        if (requestHeaders.TryGetValue(ManagementOptions.UseStatusCodeFromResponseHeaderName, out StringValues headerValue) &&
            bool.TryParse(headerValue, out bool useStatusCode))
        {
            return useStatusCode;
        }

        return null;
    }

    private static int GetStatusCode(HealthEndpointResponse response)
    {
        return response.Status is HealthStatus.Down or HealthStatus.OutOfService ? 503 : 200;
    }
}
