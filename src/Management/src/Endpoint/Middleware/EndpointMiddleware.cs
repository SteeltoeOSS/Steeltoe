// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Metrics;

namespace Steeltoe.Management.Endpoint.Middleware;

public class EndpointMiddleware<TResult>
{
    protected ILogger logger;
    protected IManagementOptions managementOptions;

    public IEndpoint<TResult> Endpoint { get; set; }

    public EndpointMiddleware(IManagementOptions managementOptions, ILogger logger = null)
    {
        ArgumentGuard.NotNull(managementOptions);

        this.logger = logger;
        this.managementOptions = managementOptions;

        if (this.managementOptions is ManagementEndpointOptions options)
        {
            options.SerializerOptions = GetSerializerOptions(options.SerializerOptions);
        }
    }

    public EndpointMiddleware(IEndpoint<TResult> endpoint, IManagementOptions managementOptions, ILogger logger = null)
        : this(managementOptions, logger)
    {
        ArgumentGuard.NotNull(endpoint);

        Endpoint = endpoint;
    }

    public virtual string HandleRequest()
    {
        TResult result = Endpoint.Invoke();
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
    public new IEndpoint<TResult, TRequest> Endpoint { get; set; }

    public EndpointMiddleware(IEndpoint<TResult, TRequest> endpoint, IManagementOptions managementOptions, ILogger logger = null)
        : base(managementOptions, logger)
    {
        ArgumentGuard.NotNull(endpoint);

        Endpoint = endpoint;
    }

    public EndpointMiddleware(IManagementOptions managementOptions, ILogger logger = null)
        : base(managementOptions, logger)
    {
    }

    public virtual string HandleRequest(TRequest arg)
    {
        TResult result = Endpoint.Invoke(arg);
        return Serialize(result);
    }
}
