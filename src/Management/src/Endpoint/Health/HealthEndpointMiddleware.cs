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
        ArgumentGuard.NotNull(endpointOptions);
        _logger = loggerFactory.CreateLogger<HealthEndpointMiddleware>();
        _healthEndpointOptionsMonitor = endpointOptions;
    }

    private HealthEndpointRequest GetRequest(HttpContext context)
    {
        return new HealthEndpointRequest
        {
            GroupName = GetRequestedHealthGroup(context),
            HasClaim = GetClaim(context)
        };
    }

    private bool GetClaim(HttpContext context)
    {
        EndpointClaim claim = _healthEndpointOptionsMonitor.CurrentValue.Claim;
        return context != null && claim != null && context.User.HasClaim(claim.Type, claim.Value);
    }

    /// <summary>
    /// Returns the last value returned by <see cref="HttpContext.Request" />.Path, expected to be the name of a configured health group.
    /// </summary>
    /// <param name="context">
    /// Last value of <see cref="HttpContext.Request" />.Path is used as group name.
    /// </param>
    private string GetRequestedHealthGroup(HttpContext context)
    {
        string[] requestComponents = context.Request.Path.Value?.Split('/') ?? Array.Empty<string>();

        if (requestComponents.Length > 0)
        {
            return requestComponents[^1];
        }

        _logger?.LogWarning("Failed to find anything in the request from which to parse health group name.");

        return string.Empty;
    }

    protected override async Task<HealthEndpointResponse> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        return await EndpointHandler.InvokeAsync(GetRequest(context), context.RequestAborted);
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
