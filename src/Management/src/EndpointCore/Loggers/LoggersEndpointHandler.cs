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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.EndpointCore.ContentNegotiation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Loggers
{
    public class LoggersEndpointHandler : EndpointHandler<Dictionary<string, object>, LoggersChangeRequest>
    {
        private readonly RequestDelegate _next;

        public LoggersEndpointHandler(RequestDelegate next, LoggersEndpoint endpoint, IManagementOptions mgmtOptions, ILogger<LoggersEndpointMiddleware> logger = null)
            : base(endpoint, mgmtOptions, logger)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if(_endpoint.ShouldInvoke(_mgmtOptions))
            {
                await HandleLoggersRequestAsync(context).ConfigureAwait(false);
            }
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
            var serialInfo = this.HandleRequest(null);
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
