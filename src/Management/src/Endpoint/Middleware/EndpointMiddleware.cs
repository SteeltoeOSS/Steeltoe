// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Trace;

namespace Steeltoe.Management.Endpoint.Middleware;

public abstract class EndpointMiddleware<TArgument, TResult> : IEndpointMiddleware
{
    private readonly ILogger _logger;
    protected IOptionsMonitor<ManagementEndpointOptions> ManagementEndpointOptionsMonitor { get; }
    protected IEndpointHandler<TArgument, TResult> EndpointHandler { get; }

    public HttpMiddlewareOptions EndpointOptions { get; }

    protected EndpointMiddleware(IEndpointHandler<TArgument, TResult> endpointHandler, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(endpointHandler);
        ArgumentGuard.NotNull(managementOptions);
        ArgumentGuard.NotNull(loggerFactory);

        EndpointHandler = endpointHandler;
        ManagementEndpointOptionsMonitor = managementOptions;
        EndpointOptions = EndpointHandler.Options;

        _logger = loggerFactory.CreateLogger<EndpointMiddleware<TArgument, TResult>>();
    }

    public virtual bool ShouldInvoke(PathString requestPath)
    {
        ArgumentGuard.NotNull(requestPath);

        ManagementEndpointOptions endpointOptions = ManagementEndpointOptionsMonitor.CurrentValue;
        bool isEnabled = EndpointHandler.Options.IsEnabled(endpointOptions);
        bool isExposed = EndpointHandler.Options.IsExposed(endpointOptions);

        bool isCloudFoundryEndpoint = requestPath.StartsWithSegments(ConfigureManagementEndpointOptions.DefaultCloudFoundryPath);
        bool returnValue = isCloudFoundryEndpoint ? endpointOptions.IsCloudFoundryEnabled && isEnabled : isEnabled && isExposed;

        _logger.LogDebug($"Returned {returnValue} for endpointHandler: {EndpointHandler.Options.Id}, requestPath: {requestPath}, isEnabled: {isEnabled}, " +
            $"isExposed: {isExposed}, isCloudFoundryEndpoint: {isCloudFoundryEndpoint}, isCloudFoundryEnabled: {endpointOptions.IsCloudFoundryEnabled}");

        return returnValue;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
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

        JsonSerializerOptions options = GetSerializerOptions();
        await JsonSerializer.SerializeAsync(context.Response.Body, result, options, cancellationToken);
    }

    protected virtual JsonSerializerOptions GetSerializerOptions()
    {
        JsonSerializerOptions serializerOptions = ManagementEndpointOptionsMonitor.CurrentValue.SerializerOptions;

        serializerOptions ??= new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        if (serializerOptions.DefaultIgnoreCondition != JsonIgnoreCondition.WhenWritingNull)
        {
            serializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        }

        if (!serializerOptions.Converters.Any(c => c is JsonStringEnumConverter))
        {
            serializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        if (!serializerOptions.Converters.Any(c => c is HealthConverter or HealthConverterV3))
        {
            serializerOptions.Converters.Add(new HealthConverter());
        }

        if (!serializerOptions.Converters.Any(c => c is HttpTraceResultConverter))
        {
            serializerOptions.Converters.Add(new HttpTraceResultConverter());
        }

        return serializerOptions;
    }
}
