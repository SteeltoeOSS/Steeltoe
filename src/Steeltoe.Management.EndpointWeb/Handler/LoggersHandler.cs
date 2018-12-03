// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Security;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web;

namespace Steeltoe.Management.Endpoint.Handler
{
    public class LoggersHandler : ActuatorHandler<LoggersEndpoint, Dictionary<string, object>, LoggersChangeRequest>
    {
        public LoggersHandler(LoggersEndpoint endpoint, ISecurityService securityService, ILogger<LoggersHandler> logger = null)
            : base(endpoint, securityService, new List<HttpMethod> { HttpMethod.Get, HttpMethod.Post }, false, logger)
        {
        }

        public override void HandleRequest(HttpContext context)
        {
            _logger?.LogTrace("Processing {SteeltoeEndpoint} request", typeof(LoggersEndpoint).Name);
            if (context.Request.HttpMethod == "GET")
            {
                // GET request
                var endpointResponse = _endpoint.Invoke(null);
                _logger?.LogTrace("Returning: {EndpointResponse}", endpointResponse);
                context.Response.Headers.Set("Content-Type", "application/vnd.spring-boot.actuator.v1+json");
                context.Response.Write(Serialize(endpointResponse));
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                return;
            }
            else
            {
                // POST - change a logger level
                _logger?.LogDebug("Incoming logger path: {0}", context.Request.Path);

                var psPath = context.Request.Path;

                foreach (string epPath in _endpoint.Paths)
                {
                    if (psPath.StartsWithSegments(epPath, out string remaining))
                    {
                        if (!string.IsNullOrEmpty(remaining))
                        {
                            string loggerName = remaining.TrimStart('/');

                            var change = ((LoggersEndpoint)_endpoint).DeserializeRequest(context.Request.InputStream);

                            change.TryGetValue("configuredLevel", out string level);

                            _logger?.LogDebug("Change Request: {Logger}, {Level}", loggerName, level ?? "RESET");
                            if (!string.IsNullOrEmpty(loggerName))
                            {
                                var changeReq = new LoggersChangeRequest(loggerName, level);
                                _endpoint.Invoke(changeReq);
                                context.Response.StatusCode = (int)HttpStatusCode.OK;
                                return;
                            }
                        }
                    }
                }

                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
    }
}
