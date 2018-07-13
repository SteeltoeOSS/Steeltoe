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
        private SecurityBase _base;

        public CloudFoundrySecurityMiddleware(RequestDelegate next, ICloudFoundryOptions options, ILogger<CloudFoundrySecurityMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _options = options;
            _base = new SecurityBase(options, logger);
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("Invoke({0})", context.Request.Path.Value);

            if (Platform.IsCloudFoundry && _options.IsEnabled && _base.IsCloudFoundryRequest(context.Request.Path))
            {
                if (string.IsNullOrEmpty(_options.ApplicationId))
                {
                    await ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, _base.APPLICATION_ID_MISSING_MESSAGE));
                    return;
                }

                if (string.IsNullOrEmpty(_options.CloudFoundryApi))
                {
                    await ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, _base.CLOUDFOUNDRY_API_MISSING_MESSAGE));
                    return;
                }

                IEndpointOptions target = FindTargetEndpoint(context.Request.Path);
                if (target == null)
                {
                    await ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, _base.ENDPOINT_NOT_CONFIGURED_MESSAGE));
                    return;
                }

                var sr = await GetPermissions(context);
                if (sr.Code != HttpStatusCode.OK)
                {
                    await ReturnError(context, sr);
                    return;
                }

                var permissions = sr.Permissions;
                if (!target.IsAccessAllowed(permissions))
                {
                    await ReturnError(context, new SecurityResult(HttpStatusCode.Forbidden, _base.ACCESS_DENIED_MESSAGE));
                    return;
                }
            }

            await _next(context);
        }

        internal string GetAccessToken(HttpRequest request)
        {
            if (request.Headers.TryGetValue(_base.AUTHORIZATION_HEADER, out StringValues headerVal))
            {
                string header = headerVal.ToString();
                if (header.StartsWith(_base.BEARER, StringComparison.OrdinalIgnoreCase))
                {
                    return header.Substring(_base.BEARER.Length + 1);
                }
            }

            return null;
        }

        internal async Task<SecurityResult> GetPermissions(HttpContext context)
        {
            string token = GetAccessToken(context.Request);
            return await _base.GetPermissionsAsync(token);
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

        private async Task ReturnError(HttpContext context, SecurityResult error)
        {
            LogError(context, error);
            context.Response.Headers.Add("Content-Type", "application/json;charset=UTF-8");
            context.Response.StatusCode = (int)error.Code;
            await context.Response.WriteAsync(_base.Serialize(error));
        }

        private void LogError(HttpContext context, SecurityResult error)
        {
            _logger.LogError("Actuator Security Error: {0} - {1}", error.Code, error.Message);
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                foreach (var header in context.Request.Headers)
                {
                    _logger.LogTrace("Header: {0} - {1}", header.Key, header.Value);
                }
            }
        }
    }
}
