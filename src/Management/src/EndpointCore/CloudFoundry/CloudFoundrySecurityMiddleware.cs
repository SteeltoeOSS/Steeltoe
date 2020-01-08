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
using Microsoft.Extensions.Primitives;
using Steeltoe.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    public class CloudFoundrySecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CloudFoundrySecurityMiddleware> _logger;
        private readonly ICloudFoundryOptions _options;
        private readonly IManagementOptions _mgmtOptions;
        private readonly SecurityBase _base;

        public CloudFoundrySecurityMiddleware(RequestDelegate next, ICloudFoundryOptions options, IEnumerable<IManagementOptions> mgmtOptions, ILogger<CloudFoundrySecurityMiddleware> logger = null)
        {
            _next = next;
            _logger = logger;
            _options = options;
            _mgmtOptions = mgmtOptions?.OfType<CloudFoundryManagementOptions>().SingleOrDefault();

            _base = new SecurityBase(options, _mgmtOptions, logger);
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("Invoke({0}) contextPath: {1}", context.Request.Path.Value, _mgmtOptions.Path);

            bool isEndpointExposed = _mgmtOptions == null ? true : _options.IsExposed(_mgmtOptions);

            if (Platform.IsCloudFoundry
                && isEndpointExposed
                && _base.IsCloudFoundryRequest(context.Request.Path))
            {
                if (string.IsNullOrEmpty(_options.ApplicationId))
                {
                    await ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, _base.APPLICATION_ID_MISSING_MESSAGE)).ConfigureAwait(false);
                    return;
                }

                if (string.IsNullOrEmpty(_options.CloudFoundryApi))
                {
                    await ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, _base.CLOUDFOUNDRY_API_MISSING_MESSAGE)).ConfigureAwait(false);
                    return;
                }

                IEndpointOptions target = FindTargetEndpoint(context.Request.Path);
                if (target == null)
                {
                    await ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, _base.ENDPOINT_NOT_CONFIGURED_MESSAGE)).ConfigureAwait(false);
                    return;
                }

                var sr = await GetPermissions(context).ConfigureAwait(false);
                if (sr.Code != HttpStatusCode.OK)
                {
                    await ReturnError(context, sr).ConfigureAwait(false);
                    return;
                }

                var permissions = sr.Permissions;
                if (!target.IsAccessAllowed(permissions))
                {
                    await ReturnError(context, new SecurityResult(HttpStatusCode.Forbidden, _base.ACCESS_DENIED_MESSAGE)).ConfigureAwait(false);
                    return;
                }
            }

            await _next(context).ConfigureAwait(false);
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
            return await _base.GetPermissionsAsync(token).ConfigureAwait(false);
        }

        private IEndpointOptions FindTargetEndpoint(PathString path)
        {
            List<IEndpointOptions> configEndpoints;

            configEndpoints = _mgmtOptions.EndpointOptions;
            foreach (var ep in configEndpoints)
            {
                var contextPath = _mgmtOptions.Path;
                if (!contextPath.EndsWith("/") && !string.IsNullOrEmpty(ep.Path))
                {
                    contextPath += "/";
                }

                var fullPath = contextPath + ep.Path;
                if (path.StartsWithSegments(new PathString(fullPath)))
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
            await context.Response.WriteAsync(_base.Serialize(error)).ConfigureAwait(false);
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
