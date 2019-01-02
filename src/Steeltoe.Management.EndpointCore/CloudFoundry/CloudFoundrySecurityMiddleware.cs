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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    public class CloudFoundrySecurityMiddleware
    {
        private RequestDelegate _next;
        private ILogger<CloudFoundrySecurityMiddleware> _logger;
        private ICloudFoundryOptions _options;
        private SecurityHelper _helper;

        public CloudFoundrySecurityMiddleware(RequestDelegate next, ICloudFoundryOptions options, ILogger<CloudFoundrySecurityMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _options = options;
            _helper = new SecurityHelper(options, logger);
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("Invoke({0})", context.Request.Path.Value);

            if (Platform.IsCloudFoundry && _options.IsEnabled && _helper.IsCloudFoundryRequest(context.Request.Path))
            {
                if (string.IsNullOrEmpty(_options.ApplicationId))
                {
                    await _helper.ReturnError(
                        context,
                        new SecurityResult(HttpStatusCode.ServiceUnavailable, _helper.APPLICATION_ID_MISSING_MESSAGE));
                    return;
                }

                if (string.IsNullOrEmpty(_options.CloudFoundryApi))
                {
                    await _helper.ReturnError(
                        context,
                        new SecurityResult(HttpStatusCode.ServiceUnavailable, _helper.CLOUDFOUNDRY_API_MISSING_MESSAGE));
                    return;
                }

                IEndpointOptions target = FindTargetEndpoint(context.Request.Path);
                if (target == null)
                {
                    await _helper.ReturnError(
                        context,
                        new SecurityResult(HttpStatusCode.ServiceUnavailable, _helper.ENDPOINT_NOT_CONFIGURED_MESSAGE));
                    return;
                }

                var sr = await GetPermissions(context);
                if (sr.Code != HttpStatusCode.OK)
                {
                    await _helper.ReturnError(context, sr);
                    return;
                }

                var permissions = sr.Permissions;
                if (!target.IsAccessAllowed(permissions))
                {
                    await _helper.ReturnError(
                        context,
                        new SecurityResult(HttpStatusCode.Forbidden, _helper.ACCESS_DENIED_MESSAGE));
                    return;
                }
            }

            await _next(context);
        }

        internal string GetAccessToken(HttpRequest request)
        {
            if (request.Headers.TryGetValue(_helper.AUTHORIZATION_HEADER, out StringValues headerVal))
            {
                string header = headerVal.ToString();
                if (header.StartsWith(_helper.BEARER, StringComparison.OrdinalIgnoreCase))
                {
                    return header.Substring(_helper.BEARER.Length + 1);
                }
            }

            return null;
        }

        internal async Task<SecurityResult> GetPermissions(HttpContext context)
        {
            string token = GetAccessToken(context.Request);
            return await _helper.GetPermissionsAsync(token);
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
