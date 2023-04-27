// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Hypermedia;

internal sealed class ActuatorHypermediaEndpointMiddleware : EndpointMiddleware<Links, string>
{
    public ActuatorHypermediaEndpointMiddleware(IActuatorEndpoint endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        ILogger<ActuatorHypermediaEndpointMiddleware> logger)
        : base(endpoint, managementOptions, logger)
    {
    }

    public override async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentGuard.NotNull(context);
        Logger.LogDebug("InvokeAsync({method}, {path})", context.Request.Method, context.Request.Path.Value);

        if (Endpoint.Options.ShouldInvoke(ManagementOptions, context, Logger))
        {
            context.HandleContentNegotiation(Logger);
            await HandleRequestAsync(context.RequestAborted, context.Response.Body, GetRequestUri(context.Request), Logger);
            Logger.LogDebug("Returning serialized response");
          
        }
    }

    private static string GetRequestUri(HttpRequest request)
    {
        string scheme = request.Scheme;

        if (request.Headers.TryGetValue("X-Forwarded-Proto", out StringValues headerScheme))
        {
            scheme = headerScheme.ToString();
        }

        // request.Host automatically includes or excludes the port based on whether it is standard for the scheme
        // ... except when we manually change the scheme to match the X-Forwarded-Proto
        if (scheme == "https" && request.Host.Port == 443)
        {
            return $"{scheme}://{request.Host.Host}{request.PathBase}{request.Path}";
        }

        return $"{scheme}://{request.Host}{request.PathBase}{request.Path}";
    }

    private async Task HandleRequestAsync(CancellationToken cancellationToken, Stream responseStream, string requestUri, ILogger logger)
    {
        Links result = await Endpoint.InvokeAsync(cancellationToken, requestUri);
        await SerializeAsync(result, responseStream, logger);
    }

    private static async Task SerializeAsync<TResult>(TResult result, Stream responseStream, ILogger logger)
    {
        try
        {
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            await JsonSerializer.SerializeAsync(responseStream, result, serializeOptions);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error serializing {MiddlewareResponse}", result);
        }
    }
}
