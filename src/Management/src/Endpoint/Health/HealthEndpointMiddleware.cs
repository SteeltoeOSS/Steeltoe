// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint.Health;

internal sealed class HealthEndpointMiddleware : EndpointMiddleware<HealthEndpointResponse, HealthEndpointRequest>
{
    IOptionsMonitor<HealthEndpointOptions> _healthEndpointOptions;

    public HealthEndpointMiddleware(IOptionsMonitor<ManagementEndpointOptions> managementOptions, IHealthEndpointHandler endpoint,
        IOptionsMonitor<HealthEndpointOptions> endpointOptions,
        ILogger<HealthEndpointMiddleware> logger)
        : base(endpoint, managementOptions, endpointOptions, logger)
    {
        ArgumentGuard.NotNull(endpointOptions);
        _healthEndpointOptions = endpointOptions;
    }


    //public override Task InvokeAsync(HttpContext context, RequestDelegate next)
    //{
    //    if (EndpointOptions.CurrentValue.ShouldInvoke(ManagementOptions, context, Logger))
    //    {
    //        return HandleHealthRequestAsync(context);
    //    }

    //    return Task.CompletedTask;
    //}

    //internal async Task HandleHealthRequestAsync(HttpContext context)
    //{
    //    string serialInfo = await DoRequestAsync(context);
    //    Logger.LogDebug("Returning: {info}", serialInfo);

    //    context.HandleContentNegotiation(Logger);
    //    await context.Response.WriteAsync(serialInfo);
    //}

    //internal async Task<string> DoRequestAsync(HttpContext context)
    //{
    //    HealthEndpointResponse result = await ((HealthEndpoint)EndpointHandler).InvokeAsync(GetRequest(context), context.RequestAborted);

    //    ManagementEndpointOptions currentOptions = ManagementOptions.CurrentValue;

    //    if (currentOptions.UseStatusCodeFromResponse)
    //    {
    //        context.Response.StatusCode = ((HealthEndpoint)EndpointHandler).GetStatusCode(result);
    //    }

    //    return Serialize(result);
    //}
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
        var claim = _healthEndpointOptions.CurrentValue.Claim;
        return context != null && context.User != null &&  claim != null && context.User.HasClaim(claim.Type, claim.Value);
    }

    /// <summary>
    /// Returns the last value returned by <see cref="HttpContext.Request.Path" />, expected to be the name of a configured health group.
    /// </summary>
    /// <param name="context">
    /// Last value of <see cref="HttpContext.Request.Path" /> is used as group name.
    /// </param>
    private string GetRequestedHealthGroup(HttpContext context)
    {
        string[] requestComponents = context.Request.Path.Value.Split('/');

        if (requestComponents != null && requestComponents.Length > 0)
        {
            return requestComponents[^1];
        }

        Logger?.LogWarning("Failed to find anything in the request from which to parse health group name.");

        return string.Empty;
    }

    public override bool ShouldInvoke(HttpContext context)
    {
        throw new NotImplementedException();
    }

    protected override Task<HealthEndpointRequest> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
