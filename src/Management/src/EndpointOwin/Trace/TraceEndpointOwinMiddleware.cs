// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Trace;
using System;
using System.Collections.Generic;
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
                await Next.Invoke(context).ConfigureAwait(false);
            }
            else
            {
                _logger?.LogTrace("Processing {SteeltoeEndpoint} request", _endpoint.GetType());
                var result = _endpoint.Invoke();
                context.Response.Headers.SetValues("Content-Type", new string[] { "application/vnd.spring-boot.actuator.v1+json" });
                await context.Response.WriteAsync(Serialize(result)).ConfigureAwait(false);
            }
        }
    }
}
