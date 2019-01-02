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
using Steeltoe.Common;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointOwin.CloudFoundry
{
    public class CloudFoundrySecurityOwinMiddleware : OwinMiddleware
    {
        private ILogger<CloudFoundrySecurityOwinMiddleware> _logger;
        private ICloudFoundryOptions _options;
        private SecurityHelper _helper;

        public CloudFoundrySecurityOwinMiddleware(OwinMiddleware next, ICloudFoundryOptions options, ILogger<CloudFoundrySecurityOwinMiddleware> logger = null)
            : base(next)
        {
            _options = options;
            _logger = logger;
            _helper = new SecurityHelper(options, logger);
        }

        public override async Task Invoke(IOwinContext context)
        {
            // if running on Cloud Foundry, security is enabled, the path starts with /cloudfoundryapplication...
            if (Platform.IsCloudFoundry && _options.IsEnabled && _helper.IsCloudFoundryRequest(context.Request.Path.ToString()))
            {
                context.Response.Headers.Set("Access-Control-Allow-Credentials", "true");
                context.Response.Headers.Set("Access-Control-Allow-Origin", context.Request.Headers.Get("origin"));
                context.Response.Headers.Set("Access-Control-Allow-Headers", "Authorization,X-Cf-App-Instance,Content-Type");

                // don't run security for a CORS request, do return 204
                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                    return;
                }

                _logger?.LogTrace("Beginning Cloud Foundry Security Processing");

                // identify the application so we can confirm the user making the request has permission
                if (string.IsNullOrEmpty(_options.ApplicationId))
                {
                    await _helper.ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, _helper.APPLICATION_ID_MISSING_MESSAGE));
                    return;
                }

                // make sure we know where to get user permissions
                if (string.IsNullOrEmpty(_options.CloudFoundryApi))
                {
                    await _helper.ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, _helper.CLOUDFOUNDRY_API_MISSING_MESSAGE));
                    return;
                }

                _logger?.LogTrace("Identifying which endpoint the request at {EndpointRequestPath} is for", context.Request.Path);
                IEndpointOptions target = FindTargetEndpoint(context.Request.Path);
                if (target == null)
                {
                    await _helper.ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, _helper.ENDPOINT_NOT_CONFIGURED_MESSAGE));
                    return;
                }

                _logger?.LogTrace("Getting user permissions");
                var sr = await GetPermissions(context);
                if (sr.Code != HttpStatusCode.OK)
                {
                    await _helper.ReturnError(context, sr);
                    return;
                }

                _logger?.LogTrace("Applying user permissions to request");
                var permissions = sr.Permissions;
                if (!target.IsAccessAllowed(permissions))
                {
                    await _helper.ReturnError(context, new SecurityResult(HttpStatusCode.Forbidden, _helper.ACCESS_DENIED_MESSAGE));
                    return;
                }

                _logger?.LogTrace("Access granted!");
            }

            await Next.Invoke(context);
        }

        internal async Task<SecurityResult> GetPermissions(IOwinContext context)
        {
            string token = GetAccessToken(context.Request);
            return await _helper.GetPermissionsAsync(token);
        }

        internal string GetAccessToken(IOwinRequest request)
        {
            if (request.Headers.TryGetValue(_helper.AUTHORIZATION_HEADER, out string[] headerVal))
            {
                string header = headerVal[0];
                if (header?.StartsWith(_helper.BEARER, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return header.Substring(_helper.BEARER.Length + 1);
                }
            }

            return null;
        }

        private IEndpointOptions FindTargetEndpoint(PathString path)
        {
            var configEndpoints = this._options.Global.EndpointOptions;
            foreach (var ep in configEndpoints)
            {
                PathString epPath = new PathString(ep.Path);
                if (path.StartsWithSegments(epPath))
                {
                    return ep;
                }
            }

            return null;
        }
    }
}
