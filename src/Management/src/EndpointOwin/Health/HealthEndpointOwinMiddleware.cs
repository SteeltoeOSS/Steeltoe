// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public HealthEndpointOwinMiddleware(OwinMiddleware next, HealthEndpoint endpoint, ILogger<HealthEndpointOwinMiddleware> logger = null)
            : base(next, endpoint, logger: logger)
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
                _logger?.LogTrace("Processing {SteeltoeEndpoint} request", typeof(HealthEndpoint));
                var result = _endpoint.Invoke(new OwinSecurityContext(context));
                context.Response.Headers.SetValues("Content-Type", new string[] { "application/vnd.spring-boot.actuator.v2+json;charset-UTF-8" });

                if (((HealthEndpointOptions)_mgmtOptions.FirstOrDefault().EndpointOptions.FirstOrDefault(o => o is HealthEndpointOptions)).HttpStatusFromHealth)
                {
                    context.Response.StatusCode = ((HealthEndpoint)_endpoint).GetStatusCode(result);
                }

                await context.Response.WriteAsync(Serialize(result)).ConfigureAwait(false);
            }
        }
    }
}
