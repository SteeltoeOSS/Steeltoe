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
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointOwin.Health
{
    public class HealthEndpointOwinMiddleware : EndpointOwinMiddleware<HealthCheckResult, ISecurityContext>
    {
        public HealthEndpointOwinMiddleware(OwinMiddleware next, HealthEndpoint endpoint, IEnumerable<IManagementOptions> mgmtOptions, ILogger<HealthEndpointOwinMiddleware> logger = null)
            : base(next, endpoint, mgmtOptions, logger: logger)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        [Obsolete]
        public HealthEndpointOwinMiddleware(OwinMiddleware next, HealthEndpoint endpoint, ILogger<HealthEndpointOwinMiddleware> logger = null)
            : base(next, endpoint, logger: logger)
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
                _logger?.LogTrace("Processing {SteeltoeEndpoint} request", typeof(HealthEndpoint));
                var result = _endpoint.Invoke(new OwinSecurityContext(context));
                context.Response.Headers.SetValues("Content-Type", new string[] { "application/vnd.spring-boot.actuator.v2+json" });
                context.Response.StatusCode = ((HealthEndpoint)_endpoint).GetStatusCode(result);
                await context.Response.WriteAsync(Serialize(result));
            }
        }
    }
}
