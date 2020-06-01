// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointOwin.Hypermedia
{
#pragma warning disable CS0618 // Type or member is obsolete
    public class ActuatorHypermediaEndpointOwinMiddleware : EndpointOwinMiddleware<Links, string>
#pragma warning restore CS0618 // Type or member is obsolete
    {
        public ActuatorHypermediaEndpointOwinMiddleware(OwinMiddleware next, ActuatorEndpoint endpoint, IEnumerable<IManagementOptions> mgmtOptions = null, ILogger<ActuatorHypermediaEndpointOwinMiddleware> logger = null)
            : base(next, endpoint, mgmtOptions?.OfType<ActuatorManagementOptions>(), logger: logger)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (!IsCloudFoundryRequest(context))
            {
                await Next.Invoke(context).ConfigureAwait(false);
            }
            else
            {
                var endpointResponse = _endpoint.Invoke(GetRequestUri(context.Request));
                _logger?.LogTrace("Returning: {EndpointResponse}", endpointResponse);
                context.Response.Headers.SetValues("Content-Type", new string[] { "application/json;charset=UTF-8" });
                await context.Response.WriteAsync(Serialize(endpointResponse)).ConfigureAwait(false);
            }
        }

        protected internal string GetRequestUri(IOwinRequest request)
        {
            string scheme = request.Scheme;

            if (request.Headers.TryGetValue("X-Forwarded-Proto", out string[] headerScheme))
            {
                scheme = headerScheme.First(); // .ToString()
            }

            return scheme + "://" + request.Host.ToString() + request.Path.ToString();
        }

        private bool IsCloudFoundryRequest(IOwinContext context)
        {
            var methodMatch = context.Request.Method == "GET";
            var endpointPaths = new List<string>();
            endpointPaths.AddRange(
                   _mgmtOptions.Select(opt =>
                   {
                       var contextPath = opt.Path;
                       if (!contextPath.EndsWith("/") && !string.IsNullOrEmpty(_endpoint.Id))
                       {
                           contextPath += "/";
                       }

                       var fullPath = contextPath + _endpoint.Path;
                       return fullPath;
                   }));

            var pathMatch = endpointPaths.Any(p => context.Request.Path.Value.Equals(p));
            return methodMatch && pathMatch;
        }
    }
}
