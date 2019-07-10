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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class MetricsEndpointMiddleware : EndpointMiddleware<IMetricsResponse, MetricsRequest>
    {
        private readonly RequestDelegate _next;

        public MetricsEndpointMiddleware(RequestDelegate next, MetricsEndpoint endpoint, IEnumerable<IManagementOptions> mgmtOptions, ILogger<MetricsEndpointMiddleware> logger = null)
            : base(endpoint, mgmtOptions, null, false, logger)
        {
            _next = next;
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public MetricsEndpointMiddleware(RequestDelegate next, MetricsEndpoint endpoint, ILogger<MetricsEndpointMiddleware> logger = null)
            : base(endpoint, null, false, logger)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (RequestVerbAndPathMatch(context.Request.Method, context.Request.Path.Value))
            {
                await HandleMetricsRequestAsync(context).ConfigureAwait(false);
            }
            else
            {
                await _next(context).ConfigureAwait(false);
            }
        }

        public override string HandleRequest(MetricsRequest arg)
        {
            var result = _endpoint.Invoke(arg);
            return result == null ? null : Serialize(result);
        }

        protected internal async Task HandleMetricsRequestAsync(HttpContext context)
        {
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

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
                response.Headers.Add("Content-Type", "application/vnd.spring-boot.actuator.v2+json");
                response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync(serialInfo).ConfigureAwait(false);
            }
        }

        protected internal string GetMetricName(HttpRequest request)
        {
            if (_mgmtOptions == null)
            {
                return GetMetricName(request, _endpoint.Path);
            }

            var paths = new List<string>(_mgmtOptions.Select(opt => $"{opt.Path}/{_endpoint.Id}"));
            foreach (var path in paths)
            {
                var metricName = GetMetricName(request, path);
                if (metricName != null)
                {
                    return metricName;
                }
            }

            return null;
        }

        protected internal List<KeyValuePair<string, string>> ParseTags(IQueryCollection query)
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

        protected internal KeyValuePair<string, string>? ParseTag(string kvp)
        {
            var str = kvp.Split(new char[] { ':' }, 2);
            if (str != null && str.Length == 2)
            {
                return new KeyValuePair<string, string>(str[0], str[1]);
            }

            return null;
        }

        private string GetMetricName(HttpRequest request, string path)
        {
            PathString epPath = new PathString(path);
            if (request.Path.StartsWithSegments(epPath, out PathString remaining) && remaining.HasValue)
            {
                return remaining.Value.TrimStart('/');
            }

            return null;
        }
    }
}
