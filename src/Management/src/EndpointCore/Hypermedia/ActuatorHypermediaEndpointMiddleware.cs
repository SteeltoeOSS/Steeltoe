// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Hypermedia
{
    public class ActuatorHypermediaEndpointMiddleware : EndpointMiddleware<Links, string>
    {
        private readonly RequestDelegate _next;

        public ActuatorHypermediaEndpointMiddleware(RequestDelegate next, ActuatorEndpoint endpoint, ActuatorManagementOptions mgmtOptions, ILogger<ActuatorHypermediaEndpointMiddleware> logger = null)
            : base(endpoint, mgmtOptions, logger: logger)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            _logger?.LogDebug("Invoke({0} {1})", context.Request.Method, context.Request.Path.Value);

            if (_endpoint.ShouldInvoke(_mgmtOptions, _logger))
            {
                var serialInfo = HandleRequest(_endpoint, GetRequestUri(context.Request), _logger);
                _logger?.LogDebug("Returning: {0}", serialInfo);

                context.HandleContentNegotiation(_logger);
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

            return $"{scheme}://{request.Host}{request.PathBase}{request.Path}";
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
                    IgnoreNullValues = true,
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
}
