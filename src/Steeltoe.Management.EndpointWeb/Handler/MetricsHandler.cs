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
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Web;

namespace Steeltoe.Management.Endpoint.Handler
{
    public class MetricsHandler : ActuatorHandler<MetricsEndpoint, IMetricsResponse, MetricsRequest>
    {
        public MetricsHandler(MetricsEndpoint endpoint, List<ISecurityService> securityServices, ILogger<MetricsHandler> logger = null)
            : base(endpoint, securityServices, null, false, logger)
        {
        }

        public override void HandleRequest(HttpContextBase context)
        {
            var request = context.Request;
            var response = context.Response;

            _logger?.LogDebug("Incoming path: {0}", request.Path);

            string metricName = GetMetricName(request);
            if (!string.IsNullOrEmpty(metricName))
            {
                // GET /metrics/{metricName}?tag=key:value&tag=key:value
                var tags = ParseTags(request.QueryString);
                var metricRequest = new MetricsRequest(metricName, tags);
                var serialInfo = HandleRequest(metricRequest);

                if (serialInfo != null)
                {
                    response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.Write(serialInfo);
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
                response.Headers.Set("Content-Type", "application/vnd.spring-boot.actuator.v1+json");
                response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.Write(serialInfo);
            }
        }

        protected internal string GetMetricName(HttpRequestBase request)
        {
            foreach (string epPath in _endpoint.Paths)
            {
                var psPath = request.Path;
                if (psPath.StartsWithSegments(epPath, out string remaining))
                {
                    if (!string.IsNullOrEmpty(remaining))
                    {
                        return remaining.TrimStart('/');
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Turn a querystring into a dictionary
        /// </summary>
        /// <param name="query">Request querystring</param>
        /// <returns>List of key-value pairs</returns>
        protected internal List<KeyValuePair<string, string>> ParseTags(NameValueCollection query)
        {
            var results = new List<KeyValuePair<string, string>>();
            if (query == null)
            {
                return results;
            }

            foreach (var q in query.AllKeys)
            {
                if (q.Equals("tag", StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (var kvp in query.GetValues(q))
                    {
                        var pair = ParseTag(kvp);
                        if (pair != null)
                        {
                            if (!results.Contains(pair.Value))
                            {
                                results.Add(pair.Value);
                            }
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
            return str != null && str.Length == 2
                ? (KeyValuePair<string, string>?)new KeyValuePair<string, string>(str[0], str[1])
                : null;
        }
    }
}
