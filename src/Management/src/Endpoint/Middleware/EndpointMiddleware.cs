// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Middleware;

public abstract class EndpointMiddleware<TArgument, TResult> : IEndpointMiddleware
{
    protected const string ContentType = "application/vnd.spring-boot.actuator.v3+json";
    private readonly ILogger _logger;
    protected IOptionsMonitor<ManagementOptions> ManagementOptionsMonitor { get; }
    protected IEndpointHandler<TArgument, TResult> EndpointHandler { get; }

    public EndpointOptions EndpointOptions => EndpointHandler.Options;

    protected EndpointMiddleware(IEndpointHandler<TArgument, TResult> endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(endpointHandler);
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        EndpointHandler = endpointHandler;
        ManagementOptionsMonitor = managementOptionsMonitor;
        _logger = loggerFactory.CreateLogger<EndpointMiddleware<TArgument, TResult>>();
    }

    public virtual bool ShouldInvoke(PathString requestPath)
    {
        ManagementOptions managementOptions = ManagementOptionsMonitor.CurrentValue;
        bool isEnabled = EndpointOptions.IsEnabled(managementOptions);
        bool isExposed = EndpointOptions.IsExposed(managementOptions);

        bool isCloudFoundryEndpoint = requestPath.StartsWithSegments(ConfigureManagementOptions.DefaultCloudFoundryPath);
        bool returnValue = isCloudFoundryEndpoint ? managementOptions.IsCloudFoundryEnabled && isEnabled : isEnabled && isExposed;

        _logger.LogDebug(
            "Returned {ReturnValue} for endpointHandler: {EndpointHandler}, requestPath: {RequestPath}, isEnabled: {IsEnabled}, " +
            "isExposed: {IsExposed}, isCloudFoundryEndpoint: {IsCloudFoundryEndpoint}, isCloudFoundryEnabled: {IsCloudFoundryEnabled}", returnValue,
            EndpointOptions.Id, requestPath, isEnabled, isExposed, isCloudFoundryEndpoint, managementOptions.IsCloudFoundryEnabled);

        return returnValue;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate? next)
    {
        ArgumentNullException.ThrowIfNull(context);

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
        ArgumentNullException.ThrowIfNull(context);

        context.Response.Headers.Append("Content-Type", ContentType);

        if (Equals(result, null))
        {
            return;
        }

        JsonSerializerOptions options = ManagementOptionsMonitor.CurrentValue.SerializerOptions;
        await JsonSerializer.SerializeAsync(context.Response.Body, result, options, cancellationToken);
    }
}
