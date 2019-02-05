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
using Microsoft.Owin;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Loggers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointOwin.Loggers
{
    public class LoggersEndpointOwinMiddleware : EndpointOwinMiddleware<Dictionary<string, object>, LoggersChangeRequest>
    {
        public LoggersEndpointOwinMiddleware(OwinMiddleware next, LoggersEndpoint endpoint, IEnumerable<IManagementOptions> mgmtOptions, ILogger<LoggersEndpointOwinMiddleware> logger = null)
         : base(next, endpoint, mgmtOptions, new List<HttpMethod> { HttpMethod.Get, HttpMethod.Post }, false, logger)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        [Obsolete]
        public LoggersEndpointOwinMiddleware(OwinMiddleware next, LoggersEndpoint endpoint, ILogger<LoggersEndpointOwinMiddleware> logger = null)
            : base(next, endpoint, new List<HttpMethod> { HttpMethod.Get, HttpMethod.Post }, false, logger)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (!RequestVerbAndPathMatch(context.Request.Method, context.Request.Path.Value))
            {
                await Next.Invoke(context);
            }
            else
            {
                _logger?.LogTrace("Processing {SteeltoeEndpoint} request", typeof(LoggersEndpoint).Name);
                if (context.Request.Method == "GET")
                {
                    // GET request
                    var endpointResponse = _endpoint.Invoke(null);
                    _logger?.LogTrace("Returning: {EndpointResponse}", endpointResponse);
                    context.Response.Headers.SetValues("Content-Type", new string[] { "application/vnd.spring-boot.actuator.v1+json" });
                    await context.Response.WriteAsync(Serialize(endpointResponse));
                }
                else
                {
                    // POST - change a logger level
                    _logger?.LogDebug("Incoming logger path: {0}", context.Request.Path.Value);
                    foreach (var path in GetPaths())
                    {
                        PathString epPath = new PathString(path);
                        if (context.Request.Path.StartsWithSegments(epPath, out PathString remaining))
                        {
                            if (remaining.HasValue)
                            {
                                string loggerName = remaining.Value.TrimStart('/');

                                var change = ((LoggersEndpoint)_endpoint).DeserializeRequest(context.Request.Body);

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

                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return;
                    }
                }
            }
        }

        private List<string> GetPaths()
        {
            var paths = new List<string>();
            if (_mgmtOptions == null)
            {
                paths.Add(_endpoint.Path);
            }
            else
            {
                foreach (var mgmt in _mgmtOptions)
                {
                    var path = mgmt.Path;
                    if (!path.EndsWith("/") && !string.IsNullOrEmpty(_endpoint.Id))
                    {
                        path += "/";
                    }

                    var fullPath = path + _endpoint.Id;
                    paths.Add(fullPath);
                }
            }

            return paths;
        }
    }
}
