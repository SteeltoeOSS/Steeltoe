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

public class EndpointMiddleware<TResult>: IEndpointMiddleware
{
    protected ILogger logger;
    protected IOptionsMonitor<ManagementEndpointOptions> managementOptions;

    //protected  managementOptions;

    public IEndpoint<TResult> Endpoint { get; set; }

    public virtual IEndpointOptions EndpointOptions => Endpoint.Options;

    public EndpointMiddleware(IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILogger logger = null)
    {
        ArgumentGuard.NotNull(managementOptions);

        this.logger = logger;
        this.managementOptions = managementOptions;

    }

    public EndpointMiddleware(IEndpoint<TResult> endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILogger logger = null)
        : this(managementOptions, logger)
    {
        ArgumentGuard.NotNull(endpoint);

        Endpoint = endpoint;
    }

    public virtual string HandleRequest(JsonSerializerOptions serializerOptions)
    {
        TResult result = Endpoint.Invoke();
        return Serialize(result, serializerOptions);
    }

    public virtual string Serialize(TResult result, JsonSerializerOptions serializerOptions)
    {
        try
        {
            JsonSerializerOptions options = GetSerializerOptions(serializerOptions);

            return JsonSerializer.Serialize(result, options);
        }
        catch (Exception e) when (e is ArgumentException or ArgumentNullException or NotSupportedException)
        {
            logger?.LogError(e, "Error serializing {MiddlewareResponse}", result);
        }

        return string.Empty;
    }

    internal JsonSerializerOptions GetSerializerOptions(JsonSerializerOptions serializerOptions)
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

    public virtual Task InvokeAsync(HttpContext context)
    {
        throw new NotImplementedException();
    }
}

public interface IEndpointMiddleware
{
    //public IManagementOptions ManagementOptions { get; }
    public IEndpointOptions EndpointOptions { get; }
    public Task InvokeAsync(HttpContext context);
}

public class EndpointMiddleware<TResult, TRequest> : EndpointMiddleware<TResult>
{
    public new IEndpoint<TResult, TRequest> Endpoint { get; set; }

    public override IEndpointOptions EndpointOptions => Endpoint.Options;

    public EndpointMiddleware(IEndpoint<TResult, TRequest> endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILogger logger = null)
        : base(managementOptions, logger)
    {
        ArgumentGuard.NotNull(endpoint);

        Endpoint = endpoint;
    }

    public EndpointMiddleware(IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILogger logger = null)
        : base(managementOptions, logger)
    {
    }

    public virtual string HandleRequest(TRequest arg, JsonSerializerOptions serializerOptions)
    {
        TResult result = Endpoint.Invoke(arg);
        return Serialize(result, serializerOptions);
    }
    public override Task InvokeAsync(HttpContext context)
    {
        return base.InvokeAsync(context);
    }
}
