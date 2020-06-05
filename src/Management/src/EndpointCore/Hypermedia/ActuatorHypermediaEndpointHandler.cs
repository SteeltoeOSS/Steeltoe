// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.EndpointCore.ContentNegotiation;
using System.Threading.Tasks;
using System.Text.Json;

namespace Steeltoe.Management.Endpoint.Hypermedia
{
    public class ActuatorHypermediaEndpointHandler : EndpointHandler<Links, string>
    {
        private readonly RequestDelegate _next;

        public ActuatorHypermediaEndpointHandler(RequestDelegate next, ActuatorEndpoint endpoint, ActuatorManagementOptions mgmtOptions, ILogger<ActuatorHypermediaEndpointMiddleware> logger = null)
            : base(endpoint, mgmtOptions, logger: logger)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            _logger?.LogDebug("Invoke({0} {1})", context.Request.Method, context.Request.Path.Value);

            if (_endpoint.ShouldInvoke(_mgmtOptions))
            {
                var serialInfo = HandleRequest(_endpoint, GetRequestUri(context.Request), _logger);
                _logger?.LogDebug("Returning: {0}", serialInfo);

                context.HandleContentNegotiation(_logger);
                await context.Response.WriteAsync(serialInfo).ConfigureAwait(false);
            }
        }

        private static string GetRequestUri(HttpRequest request)
        {
            string scheme = request.Scheme;

            if (request.Headers.TryGetValue("X-Forwarded-Proto", out StringValues headerScheme))
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

        private static string Serialize<TResult>(TResult result, ILogger _logger)
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
                _logger?.LogError("Error {Exception} serializing {MiddlewareResponse}", e, result);
            }

            return string.Empty;
        }
    }
}
