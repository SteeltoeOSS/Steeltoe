// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public LoggersEndpointOwinMiddleware(OwinMiddleware next, LoggersEndpoint endpoint, ILogger<LoggersEndpointOwinMiddleware> logger = null)
            : base(next, endpoint, new List<HttpMethod> { HttpMethod.Get, HttpMethod.Post }, false, logger)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (!RequestVerbAndPathMatch(context.Request.Method, context.Request.Path.Value))
            {
                await Next.Invoke(context).ConfigureAwait(false);
            }
            else
            {
                _logger?.LogTrace("Processing {SteeltoeEndpoint} request", typeof(LoggersEndpoint).Name);
                if (context.Request.Method == "GET")
                {
                    // GET request
                    var endpointResponse = _endpoint.Invoke(null);
                    _logger?.LogTrace("Returning: {EndpointResponse}", endpointResponse);
                    context.Response.Headers.SetValues("Content-Type", new string[] { "application/vnd.spring-boot.actuator.v2+json" });
                    await context.Response.WriteAsync(Serialize(endpointResponse)).ConfigureAwait(false);
                }
                else
                {
                    // POST - change a logger level
                    _logger?.LogDebug("Incoming logger path: {0}", context.Request.Path.Value);
                    foreach (var path in GetPaths())
                    {
                        PathString epPath = new PathString(path);
                        if (context.Request.Path.StartsWithSegments(epPath, out PathString remaining) && remaining.HasValue)
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
