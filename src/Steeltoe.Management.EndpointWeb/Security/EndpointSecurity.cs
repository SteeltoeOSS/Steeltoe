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
using Newtonsoft.Json;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Discovery;
using System;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace Steeltoe.Management.Endpoint.Security
{
    public class EndpointSecurity : ISecurityService
    {
        private const string ACCESS_DENIED = "Access denied";
        private ILogger<EndpointSecurity> _logger;
        private IActuatorDiscoveryOptions _options;
        private IManagementOptions _actuatorManagementOptions;

        public EndpointSecurity(IActuatorDiscoveryOptions options, IManagementOptions actuatorMgmtOptions, ILogger<EndpointSecurity> logger = null)
        {
            _options = options;
            _logger = logger;
            _actuatorManagementOptions = actuatorMgmtOptions;
        }

        public async Task<bool> IsAccessAllowed(HttpContextBase context, IEndpointOptions target)
        {
            if (IsActuatorRequest(context.Request.Path) && _options.IsEnabled(_actuatorManagementOptions))
            {
                _logger?.LogTrace("Beginning Endpoint Security Processing");

                var origin = context.Request.Headers.Get("Origin");
                context.Response.Headers.Set("Access-Control-Allow-Origin", origin ?? "*");

                if (target.IsSensitive(_actuatorManagementOptions) && !HasSensitiveClaim(context))
                {
                    _logger?.LogTrace("Access denied! Target was marked sensitive, but did not have claim {0}", _actuatorManagementOptions.SensitiveClaim);
                    await ReturnError(context, new SecurityResult(HttpStatusCode.Unauthorized, ACCESS_DENIED));
                    return false;
                }

                _logger?.LogTrace("Access granted!");
            }

            return true;
        }

        private bool IsActuatorRequest(string requestPath)
        {
            var contextPath = _actuatorManagementOptions.Path;
            return requestPath.StartsWith(contextPath);
        }

        private bool HasSensitiveClaim(HttpContextBase context)
        {
            var claim = _actuatorManagementOptions.SensitiveClaim;
            var user = context.User;
            return claim != null &&
                    user != null &&
                    user.Identity.IsAuthenticated && ((ClaimsIdentity)user.Identity).HasClaim(claim.Type, claim.Value);
        }

        private async Task ReturnError(HttpContextBase context, SecurityResult error)
        {
            LogError(context, error);
            context.Response.Headers.Set("Content-Type", "application/json;charset=UTF-8");
            context.Response.StatusCode = (int)error.Code;
            await context.Response.Output.WriteAsync(Serialize(error));
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

        private string Serialize(SecurityResult error)
        {
            try
            {
                return JsonConvert.SerializeObject(error);
            }
            catch (Exception e)
            {
                _logger?.LogError("Serialization Exception: {0}", e);
            }

            return string.Empty;
        }
    }
}
