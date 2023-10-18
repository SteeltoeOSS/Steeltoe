// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Services;

namespace Steeltoe.Management.Endpoint.Middleware;

public abstract class EndpointMiddleware<TArgument, TResult> : IEndpointMiddleware
{
    private readonly ILogger _logger;

    protected IOptionsMonitor<ManagementOptions> ManagementOptionsMonitor { get; }
    protected IEndpointHandler<TArgument, TResult> EndpointHandler { get; }

    public EndpointOptions EndpointOptions => EndpointHandler.Options;

    protected EndpointMiddleware(IEndpointHandler<TArgument, TResult> endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(endpointHandler);
        ArgumentGuard.NotNull(managementOptionsMonitor);
        ArgumentGuard.NotNull(loggerFactory);

        EndpointHandler = endpointHandler;
        ManagementOptionsMonitor = managementOptionsMonitor;
        _logger = loggerFactory.CreateLogger<EndpointMiddleware<TArgument, TResult>>();
    }

    public virtual bool ShouldInvoke(PathString requestPath)
    {
        ArgumentGuard.NotNull(requestPath);

        ManagementOptions managementOptions = ManagementOptionsMonitor.CurrentValue;
        bool isEnabled = EndpointOptions.IsEnabled(managementOptions);
        bool isExposed = EndpointOptions.IsExposed(managementOptions);

        bool isCloudFoundryEndpoint = requestPath.StartsWithSegments(ConfigureManagementOptions.DefaultCloudFoundryPath);
        bool returnValue = isCloudFoundryEndpoint ? managementOptions.IsCloudFoundryEnabled && isEnabled : isEnabled && isExposed;

        _logger.LogDebug($"Returned {returnValue} for endpointHandler: {EndpointOptions.Id}, requestPath: {requestPath}, isEnabled: {isEnabled}, " +
            $"isExposed: {isExposed}, isCloudFoundryEndpoint: {isCloudFoundryEndpoint}, isCloudFoundryEnabled: {managementOptions.IsCloudFoundryEnabled}");

        return returnValue;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate? next)
    {
        ArgumentGuard.NotNull(context);

        if (ShouldInvoke(context.Request.Path))
        {
            TResult result = await InvokeEndpointHandlerAsync(context, context.RequestAborted);
            await WriteResponseAsync(result, context, context.RequestAborted);
        }
        else
        {
            // Terminal middleware
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        }
    }

    protected abstract Task<TResult> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken);

    protected virtual async Task WriteResponseAsync(TResult result, HttpContext context, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(context);

        context.HandleContentNegotiation(_logger);

        if (Equals(result, null))
        {
            return;
        }

        JsonSerializerOptions options = ManagementOptionsMonitor.CurrentValue.SerializerOptions;
        await JsonSerializer.SerializeAsync(context.Response.Body, result, options, cancellationToken);
    }
}
