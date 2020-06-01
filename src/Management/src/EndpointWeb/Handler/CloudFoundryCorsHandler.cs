// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Steeltoe.Management.Endpoint.Handler
{
    public class CloudFoundryCorsHandler : ActuatorHandler
    {
        private readonly IEndpointOptions _options;

        public CloudFoundryCorsHandler(IEndpointOptions options, IEnumerable<ISecurityService> securityServices, IEnumerable<IManagementOptions> mgmtOptions, ILogger<CloudFoundryCorsHandler> logger = null)
            : base(securityServices, mgmtOptions, new List<HttpMethod> { HttpMethod.Options }, false, logger)
        {
            _options = options;
            _mgmtOptions = mgmtOptions;
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public CloudFoundryCorsHandler(CloudFoundryOptions options, IEnumerable<ISecurityService> securityServices, ILogger<CloudFoundryCorsHandler> logger = null)
           : base(securityServices, new List<HttpMethod> { HttpMethod.Options }, false, logger)
        {
            _options = options;
        }

        public override bool RequestVerbAndPathMatch(string httpMethod, string requestPath)
        {
            _logger?.LogTrace("RequestVerbAndPathMatch {httpMethod}/{requestPath}/{optionsPath} request", httpMethod, requestPath, _options.Path);
            return PathMatches(requestPath) && _allowedMethods.Any(m => m.Method.Equals(httpMethod));
        }

        public override void HandleRequest(HttpContextBase context)
        {
            _logger?.LogTrace("Processing {SteeltoeEndpoint} request", typeof(CloudFoundryCorsHandler));
            if (context.Request.HttpMethod == "OPTIONS")
            {
                var reqMethods = context.Request.Headers.Get("Access-Control-Request-Method");
                context.Response.Headers.Set("Access-Control-Allow-Methods", reqMethods ?? "GET,PUT");

                context.Response.Headers.Set("Access-Control-Allow-Origin", context.Request.Headers.Get("Origin"));

                var reqHeaders = context.Request.Headers.Get("Access-Control-Request-Headers");
                context.Response.Headers.Set("Access-Control-Allow-Headers", reqHeaders ?? "Authorization");

                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
            }
        }

        public override Task<bool> IsAccessAllowed(HttpContextBase context)
        {
            return Task.FromResult(true);
        }

        private bool PathMatches(string requestPath)
        {
            if (_mgmtOptions == null)
            {
                return requestPath.StartsWith(_options.Path);
            }
            else
            {
                foreach (var mgmt in _mgmtOptions)
                {
                    var path = mgmt.Path;
                    if (!path.EndsWith("/") && !string.IsNullOrEmpty(_options.Id))
                    {
                        path += "/";
                    }

                    var fullPath = path + _options.Id;
                    if (requestPath.StartsWith(fullPath))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
