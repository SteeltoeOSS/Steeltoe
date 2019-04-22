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
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.EndpointBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointOwin.Trace
{
    public class TraceEndpointOwinMiddleware : EndpointOwinMiddleware<List<TraceResult>>
    {
        protected new TraceEndpoint _endpoint;

        public TraceEndpointOwinMiddleware(OwinMiddleware next, TraceEndpoint endpoint, IEnumerable<IManagementOptions> mgmtOptions, ILogger<TraceEndpointOwinMiddleware> logger = null)
            : base(next, endpoint, mgmtOptions, new List<HttpMethod> { HttpMethod.Get }, true, logger: logger)
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
                _logger?.LogTrace("Processing {SteeltoeEndpoint} request", _endpoint.GetType());
                var result = _endpoint.Invoke();
                context.Response.Headers.SetValues("Content-Type", new string[] { "application/vnd.spring-boot.actuator.v1+json" });
                await context.Response.WriteAsync(Serialize(result));
            }
        }
    }
}
