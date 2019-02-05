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
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.CloudFoundry;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace Steeltoe.Management.Endpoint.Security
{
    public class CloudFoundrySecurity : ISecurityService
    {
        private ILogger<CloudFoundrySecurity> _logger;
        private ICloudFoundryOptions _options;
        private readonly IManagementOptions _managementOptions;
        private SecurityBase _base;

        public CloudFoundrySecurity(ICloudFoundryOptions options, IManagementOptions managementOptions, ILogger<CloudFoundrySecurity> logger = null)
        {
            _options = options;
            _managementOptions = managementOptions;
            _logger = logger;
            _base = new SecurityBase(options, logger);
        }

        [Obsolete]
        public CloudFoundrySecurity(ICloudFoundryOptions options, ILogger<CloudFoundrySecurity> logger = null)
        {
            _options = options;
            _logger = logger;
            _base = new SecurityBase(options, logger);
        }

        public async Task<bool> IsAccessAllowed(HttpContextBase context, IEndpointOptions target)
        {
            bool isEnabled = _managementOptions == null ? _options.IsEnabled : _options.IsEnabled(_managementOptions);
            // if running on Cloud Foundry, security is enabled, the path starts with /cloudfoundryapplication...
            if (Platform.IsCloudFoundry && isEnabled)
            {
                _logger?.LogTrace("Beginning Cloud Foundry Security Processing");

                if (context.Request.HttpMethod == "OPTIONS")
                {
                    return true;
                }

                var origin = context.Request.Headers.Get("Origin");
                context.Response.Headers.Set("Access-Control-Allow-Origin", origin ?? "*");

                // identify the application so we can confirm the user making the request has permission
                if (string.IsNullOrEmpty(_options.ApplicationId))
                {
                    await ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, _base.APPLICATION_ID_MISSING_MESSAGE));
                    return false;
                }

                // make sure we know where to get user permissions
                if (string.IsNullOrEmpty(_options.CloudFoundryApi))
                {
                    await ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, _base.CLOUDFOUNDRY_API_MISSING_MESSAGE));
                    return false;
                }

                _logger?.LogTrace("Getting user permissions");
                var sr = await GetPermissions(context);
                if (sr.Code != HttpStatusCode.OK)
                {
                    await ReturnError(context, sr);
                    return false;
                }

                _logger?.LogTrace("Applying user permissions to request");
                var permissions = sr.Permissions;
                if (!target.IsAccessAllowed(permissions))
                {
                    await ReturnError(context, new SecurityResult(HttpStatusCode.Forbidden, _base.ACCESS_DENIED_MESSAGE));
                    return false;
                }

                _logger?.LogTrace("Access granted!");
            }

            return true;
        }

        internal async Task<SecurityResult> GetPermissions(HttpContextBase context)
        {
            string token = GetAccessToken(context.Request);
            return await _base.GetPermissionsAsync(token);
        }

        internal string GetAccessToken(HttpRequestBase request)
        {
            string[] headerVals = request.Headers.GetValues(_base.AUTHORIZATION_HEADER);
            if (headerVals != null && headerVals.Length > 0)
            {
                string header = headerVals[0];
                if (header?.StartsWith(_base.BEARER, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return header.Substring(_base.BEARER.Length + 1);
                }
            }

            return null;
        }

        private async Task ReturnError(HttpContextBase context, SecurityResult error)
        {
            LogError(context, error);
            context.Response.Headers.Set("Content-Type",  "application/json;charset=UTF-8");
            context.Response.StatusCode = (int)error.Code;
            await context.Response.Output.WriteAsync(_base.Serialize(error));
        }

        private void LogError(HttpContextBase context, SecurityResult error)
        {
            _logger?.LogError("Actuator Security Error: {ErrorCode} - {ErrorMessage}", error.Code, error.Message);
            if (_logger?.IsEnabled(LogLevel.Trace) == true)
            {
                foreach (var header in context.Request.Headers.AllKeys)
                {
                    _logger?.LogTrace("Header: {HeaderKey} - {HeaderValue}", header, context.Request.Headers[header]);
                }
            }
        }
    }
}
