// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Loggers
{
    public class LoggersEndpointMiddleware : EndpointMiddleware<Dictionary<string, object>, LoggersChangeRequest>
    {
        private readonly RequestDelegate _next;

        public LoggersEndpointMiddleware(RequestDelegate next, LoggersEndpoint endpoint, IManagementOptions mgmtOptions, ILogger<LoggersEndpointMiddleware> logger = null)
            : base(endpoint, mgmtOptions, logger)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            if (_endpoint.ShouldInvoke(_mgmtOptions, _logger))
            {
                return HandleLoggersRequestAsync(context);
            }

            return Task.CompletedTask;
        }

        protected internal async Task HandleLoggersRequestAsync(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;

            if (context.Request.Method.Equals("POST"))
            {
                // POST - change a logger level
                var paths = new List<string>();
                _logger?.LogDebug("Incoming path: {0}", request.Path.Value);
                if (_mgmtOptions == null)
                {
                    paths.Add(_endpoint.Path);
                }
                else
                {
                    paths.Add($"{_mgmtOptions.Path}/{_endpoint.Path}");
                }

                foreach (var path in paths)
                {
                    if (ChangeLoggerLevel(request, path))
                    {
                        response.StatusCode = (int)HttpStatusCode.OK;
                        return;
                    }
                }

                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            // GET request
            var serialInfo = HandleRequest(null);
            _logger?.LogDebug("Returning: {0}", serialInfo);

            context.HandleContentNegotiation(_logger);
            await context.Response.WriteAsync(serialInfo).ConfigureAwait(false);
        }

        private bool ChangeLoggerLevel(HttpRequest request, string path)
        {
            var epPath = new PathString(path);
            if (request.Path.StartsWithSegments(epPath, out var remaining) && remaining.HasValue)
            {
                var loggerName = remaining.Value.TrimStart('/');

                var change = ((LoggersEndpoint)_endpoint).DeserializeRequest(request.Body);

                change.TryGetValue("configuredLevel", out var level);

                _logger?.LogDebug("Change Request: {0}, {1}", loggerName, level ?? "RESET");
                if (!string.IsNullOrEmpty(loggerName))
                {
                    var changeReq = new LoggersChangeRequest(loggerName, level);
                    HandleRequest(changeReq);
                    return true;
                }
            }

            return false;
        }
    }
}
