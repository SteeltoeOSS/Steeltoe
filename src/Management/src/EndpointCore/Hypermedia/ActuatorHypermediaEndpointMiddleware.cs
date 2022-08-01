// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using System.Text.Json;

namespace Steeltoe.Management.Endpoint.Hypermedia;

public class ActuatorHypermediaEndpointMiddleware : EndpointMiddleware<Links, string>
{
    private readonly RequestDelegate _next;

    public ActuatorHypermediaEndpointMiddleware(RequestDelegate next, ActuatorEndpoint endpoint, ActuatorManagementOptions managementOptions, ILogger<ActuatorHypermediaEndpointMiddleware> logger = null)
        : base(endpoint, managementOptions, logger: logger)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        logger?.LogDebug("Invoke({0} {1})", context.Request.Method, context.Request.Path.Value);

        if (endpoint.ShouldInvoke(managementOptions, logger))
        {
            var serialInfo = HandleRequest(endpoint, GetRequestUri(context.Request), logger);
            logger?.LogDebug("Returning: {0}", serialInfo);

            context.HandleContentNegotiation(logger);
            return context.Response.WriteAsync(serialInfo);
        }

        return Task.CompletedTask;
    }

    private static string GetRequestUri(HttpRequest request)
    {
        var scheme = request.Scheme;

        if (request.Headers.TryGetValue("X-Forwarded-Proto", out var headerScheme))
        {
            scheme = headerScheme.ToString();
        }

        // request.Host automatically includes or excludes the port based on whether it is standard for the scheme
        // ... except when we manually change the scheme to match the X-Forwarded-Proto
        if (scheme == "https" && request.Host.Port == 443)
        {
            return $"{scheme}://{request.Host.Host}{request.PathBase}{request.Path}";
        }
        else
        {
            return $"{scheme}://{request.Host}{request.PathBase}{request.Path}";
        }
    }

    private static string HandleRequest(IEndpoint<Links, string> endpoint, string requestUri, ILogger logger)
    {
        var result = endpoint.Invoke(requestUri);
        return Serialize(result, logger);
    }

    private static string Serialize<TResult>(TResult result, ILogger logger)
    {
        try
        {
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            };

            return JsonSerializer.Serialize(result, serializeOptions);
        }
        catch (Exception e)
        {
            logger?.LogError("Error {Exception} serializing {MiddlewareResponse}", e, result);
        }

        return string.Empty;
    }
}
