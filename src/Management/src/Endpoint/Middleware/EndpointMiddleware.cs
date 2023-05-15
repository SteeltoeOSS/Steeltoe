// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Middleware;

public abstract class EndpointMiddleware<TResult> : IEndpointMiddleware
{
    protected ILogger Logger { get; }
    protected IOptionsMonitor<ManagementEndpointOptions> ManagementOptions { get; }

    public IEndpoint<TResult> Endpoint { get; set; }

    public virtual IOptionsMonitor<HttpMiddlewareOptions> EndpointOptions { get; }

    protected EndpointMiddleware(IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILogger logger)
    {
        ArgumentGuard.NotNull(logger);
        Logger = logger;
        ManagementOptions = managementOptions;
    }

    protected EndpointMiddleware(IEndpoint<TResult> endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions, IOptionsMonitor<HttpMiddlewareOptions> endpointOptions, ILogger logger)
        : this(managementOptions, logger)
    {
        ArgumentGuard.NotNull(endpoint);
        ArgumentGuard.NotNull(logger);
        Endpoint = endpoint;
        EndpointOptions = endpointOptions;
    }

    public virtual async Task<string> HandleRequestAsync(CancellationToken cancellationToken)
    {
        TResult result = await Endpoint.InvokeAsync(cancellationToken);
        return await Task.Run(() => Serialize(result), cancellationToken);
    }

    public virtual string Serialize(TResult result)
    {
        try
        {
            JsonSerializerOptions serializerOptions = ManagementOptions.CurrentValue.SerializerOptions;
            JsonSerializerOptions options = GetSerializerOptions(serializerOptions);

            return JsonSerializer.Serialize(result, options);
        }
        catch (Exception e) when (e is ArgumentException or ArgumentNullException or NotSupportedException)
        {
            Logger.LogError(e, "Error serializing {MiddlewareResponse}", result);
        }

        return string.Empty;
    }

    internal static JsonSerializerOptions GetSerializerOptions(JsonSerializerOptions serializerOptions)
    {
        serializerOptions ??= new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        if (serializerOptions.DefaultIgnoreCondition != JsonIgnoreCondition.WhenWritingNull)
        {
            serializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        }

        if (serializerOptions.Converters?.Any(c => c is JsonStringEnumConverter) != true)
        {
            serializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        if (serializerOptions.Converters?.Any(c => c is HealthConverter or HealthConverterV3) != true)
        {
            serializerOptions.Converters.Add(new HealthConverter());
        }

        if (serializerOptions.Converters?.Any(c => c is MetricsResponseConverter) != true)
        {
            serializerOptions.Converters.Add(new MetricsResponseConverter());
        }

        return serializerOptions;
    }

    public abstract Task InvokeAsync(HttpContext context, RequestDelegate next);
}

public interface IEndpointMiddleware : IMiddleware
{
    public IOptionsMonitor<HttpMiddlewareOptions> EndpointOptions { get; }
}

public abstract class EndpointMiddleware<TResult, TRequest> : EndpointMiddleware<TResult>
{
    public new IEndpoint<TResult, TRequest> Endpoint { get; set; }

    public override IOptionsMonitor<HttpMiddlewareOptions> EndpointOptions { get; }

    protected EndpointMiddleware(IEndpoint<TResult, TRequest> endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions, IOptionsMonitor<HttpMiddlewareOptions> endpointOptions, ILogger logger)
        : base(managementOptions, logger)
    {
        ArgumentGuard.NotNull(endpoint);

        Endpoint = endpoint;
        EndpointOptions = endpointOptions;
    }

    //protected EndpointMiddleware(IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILogger logger)
    //    : base(managementOptions, logger)
    //{
    //}

    public virtual async Task<string> HandleRequestAsync(TRequest arg, CancellationToken cancellationToken)
    {
        TResult result = await Endpoint.InvokeAsync(arg, cancellationToken);
        return Serialize(result);
    }

    public abstract override Task InvokeAsync(HttpContext context, RequestDelegate next);
}
