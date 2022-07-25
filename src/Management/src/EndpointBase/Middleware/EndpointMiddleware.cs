// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Metrics;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Middleware;
public class EndpointMiddleware<TResult>
{
    protected IEndpoint<TResult> endpoint;
    protected ILogger logger;
    protected IManagementOptions managementOptions;

    public EndpointMiddleware(IManagementOptions managementOptions, ILogger logger = null)
    {
        this.logger = logger;
        this.managementOptions = managementOptions ?? throw new ArgumentNullException(nameof(managementOptions));
        if (this.managementOptions is ManagementEndpointOptions options)
        {
            options.SerializerOptions = GetSerializerOptions(options.SerializerOptions);
        }
    }

    public EndpointMiddleware(IEndpoint<TResult> endpoint, IManagementOptions managementOptions, ILogger logger = null)
        : this(managementOptions, logger)
    {
        this.endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
    }

    public IEndpoint<TResult> Endpoint
    {
        get => endpoint;

        set => endpoint = value;
    }

    public virtual string HandleRequest()
    {
        var result = endpoint.Invoke();
        return Serialize(result);
    }

    public virtual string Serialize(TResult result)
    {
        try
        {
            JsonSerializerOptions options;
            if (managementOptions is ManagementEndpointOptions endpointOptions)
            {
                options = endpointOptions.SerializerOptions;
            }
            else
            {
                options = GetSerializerOptions(null);
            }

            return JsonSerializer.Serialize(result, options);
        }
        catch (Exception e) when (e is ArgumentException or ArgumentNullException or NotSupportedException)
        {
            logger?.LogError("Error {Exception} serializing {MiddlewareResponse}", e, result);
        }

        return string.Empty;
    }

    internal JsonSerializerOptions GetSerializerOptions(JsonSerializerOptions serializerOptions)
    {
        serializerOptions ??= new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        serializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

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
}

public class EndpointMiddleware<TResult, TRequest> : EndpointMiddleware<TResult>
{
    protected new IEndpoint<TResult, TRequest> endpoint;

    internal new IEndpoint<TResult, TRequest> Endpoint
    {
        get => endpoint;

        set => endpoint = value;
    }

    public EndpointMiddleware(IEndpoint<TResult, TRequest> endpoint, IManagementOptions managementOptions, ILogger logger = null)
        : base(managementOptions, logger)
    {
        this.endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
    }

    public EndpointMiddleware(IManagementOptions managementOptions, ILogger logger = null)
        : base(managementOptions, logger)
    {
    }

    public virtual string HandleRequest(TRequest arg)
    {
        var result = endpoint.Invoke(arg);
        return Serialize(result);
    }
}
