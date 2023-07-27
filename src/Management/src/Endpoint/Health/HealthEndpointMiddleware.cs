// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint.Health;

internal sealed class HealthEndpointMiddleware : EndpointMiddleware<HealthEndpointRequest, HealthEndpointResponse>
{
    private readonly IOptionsMonitor<HealthEndpointOptions> _healthEndpointOptionsMonitor;
    private readonly ILogger<HealthEndpointMiddleware> _logger;

    public HealthEndpointMiddleware(IOptionsMonitor<ManagementEndpointOptions> managementOptions, IHealthEndpointHandler endpointHandler,
        IOptionsMonitor<HealthEndpointOptions> endpointOptions, ILoggerFactory loggerFactory)
        : base(endpointHandler, managementOptions, loggerFactory)
    {
        ArgumentGuard.NotNull(managementOptions);
        ArgumentGuard.NotNull(endpointHandler);
        ArgumentGuard.NotNull(endpointOptions);
        ArgumentGuard.NotNull(loggerFactory);

        _logger = loggerFactory.CreateLogger<HealthEndpointMiddleware>();
        _healthEndpointOptionsMonitor = endpointOptions;
    }

    protected override async Task<HealthEndpointResponse> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var request = new HealthEndpointRequest
        {
            GroupName = GetRequestedHealthGroup(context.Request.Path),
            HasClaim = GetHasClaim(context)
        };

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

        _logger?.LogWarning("Failed to find anything in the request from which to parse health group name.");

        return string.Empty;
    }

    private bool GetHasClaim(HttpContext context)
    {
        EndpointClaim claim = _healthEndpointOptionsMonitor.CurrentValue.Claim;
        return claim != null && context.User.HasClaim(claim.Type, claim.Value);
    }

    protected override async Task WriteResponseAsync(HealthEndpointResponse result, HttpContext context, CancellationToken cancellationToken)
    {
        ManagementEndpointOptions currentOptions = ManagementEndpointOptionsMonitor.CurrentValue;

        if (currentOptions.UseStatusCodeFromResponse)
        {
            context.Response.StatusCode = ((HealthEndpointHandler)EndpointHandler).GetStatusCode(result);
        }

        await base.WriteResponseAsync(result, context, cancellationToken);
    }
}
