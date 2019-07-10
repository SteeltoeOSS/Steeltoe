﻿// Copyright 2017 the original author or authors.
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

using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointOwin.Metrics
{
    public class MetricsEndpointOwinMiddleware : EndpointOwinMiddleware<IMetricsResponse, MetricsRequest>
    {
        protected new MetricsEndpoint _endpoint;

        public MetricsEndpointOwinMiddleware(OwinMiddleware next, MetricsEndpoint endpoint, IEnumerable<IManagementOptions> mgmtOptions, ILogger<MetricsEndpointOwinMiddleware> logger = null)
            : base(next, endpoint, mgmtOptions, null, false, logger)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public MetricsEndpointOwinMiddleware(OwinMiddleware next, MetricsEndpoint endpoint, ILogger<MetricsEndpointOwinMiddleware> logger = null)
            : base(next, endpoint, null, false, logger)
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
                await HandleMetricsRequestAsync(context).ConfigureAwait(false);
            }
        }

        public override string HandleRequest(MetricsRequest arg)
        {
            var result = _endpoint.Invoke(arg);
            return result == null ? null : Serialize(result);
        }

        protected internal async Task HandleMetricsRequestAsync(IOwinContext context)
        {
            IOwinRequest request = context.Request;
            IOwinResponse response = context.Response;

            _logger?.LogDebug("Incoming path: {0}", request.Path.Value);

            string metricName = GetMetricName(request);
            if (!string.IsNullOrEmpty(metricName))
            {
                // GET /metrics/{metricName}?tag=key:value&tag=key:value
                var tags = ParseTags(request.Query);
                var metricRequest = new MetricsRequest(metricName, tags);
                var serialInfo = HandleRequest(metricRequest);

                if (serialInfo != null)
                {
                    response.StatusCode = (int)HttpStatusCode.OK;
                    await context.Response.WriteAsync(serialInfo).ConfigureAwait(false);
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                }
            }
            else
            {
                // GET /metrics
                var serialInfo = this.HandleRequest(null);
                _logger?.LogDebug("Returning: {0}", serialInfo);
                response.Headers.SetValues("Content-Type", new string[] { "application/vnd.spring-boot.actuator.v2+json" });
                response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync(serialInfo).ConfigureAwait(false);
            }
        }

        protected internal string GetMetricName(IOwinRequest request)
        {
            var epPaths = GetEndpointPaths();
            foreach (var path in epPaths)
            {
                PathString epPath = new PathString(path);
                if (request.Path.StartsWithSegments(epPath, out PathString remaining) && remaining.HasValue)
                {
                    return remaining.Value.TrimStart('/');
                }
            }

            return null;
        }

        /// <summary>
        /// Turn a querystring into a dictionary
        /// </summary>
        /// <param name="query">Request querystring</param>
        /// <returns>List of key-value pairs</returns>
        protected internal List<KeyValuePair<string, string>> ParseTags(IReadableStringCollection query)
        {
            List<KeyValuePair<string, string>> results = new List<KeyValuePair<string, string>>();
            if (query == null)
            {
                return results;
            }

            foreach (var q in query)
            {
                if (q.Key.Equals("tag", StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (var kvp in q.Value)
                    {
                        var pair = ParseTag(kvp);
                        if (pair != null && !results.Contains(pair.Value))
                        {
                            results.Add(pair.Value);
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Split a key-value pair out from a single string
        /// </summary>
        /// <param name="kvp">Colon-delimited key-value pair</param>
        /// <returns>A pair of strings</returns>
        protected internal KeyValuePair<string, string>? ParseTag(string kvp)
        {
            var str = kvp.Split(new char[] { ':' }, 2);
            if (str != null && str.Length == 2)
            {
                return new KeyValuePair<string, string>(str[0], str[1]);
            }

            return null;
        }

        private IEnumerable<string> GetEndpointPaths()
        {
            if (_mgmtOptions == null)
            {
                return new List<string>() { _endpoint.Path };
            }
            else
            {
                return _mgmtOptions.Select(opt => $"{opt.Path}/{_endpoint.Id}");
            }
        }
    }
}
