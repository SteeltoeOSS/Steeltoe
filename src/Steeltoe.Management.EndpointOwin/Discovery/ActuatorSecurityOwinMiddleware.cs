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
using Newtonsoft.Json;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Discovery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointOwin.Discovery
{
    public class ActuatorSecurityOwinMiddleware : OwinMiddleware
    {
        protected readonly string ENDPOINT_NOT_CONFIGURED_MESSAGE = "Endpoint is not available";
        protected readonly string ACCESS_DENIED_MESSAGE = "Access denied";

        private ILogger<ActuatorSecurityOwinMiddleware> _logger;
        private IActuatorDiscoveryOptions _options;
        private IManagementOptions _actuatorManagementOptions;

        public ActuatorSecurityOwinMiddleware(OwinMiddleware next, IActuatorDiscoveryOptions options, IEnumerable<IManagementOptions> mgmtOptions, ILogger<ActuatorSecurityOwinMiddleware> logger = null)
            : base(next)
        {
            _options = options;
            _logger = logger;
            _actuatorManagementOptions = mgmtOptions.OfType<ActuatorManagementOptions>().Single();
        }

        public override async Task Invoke(IOwinContext context)
        {
            _logger?.LogDebug("Invoke({0})", context.Request.Path.Value);

            if (IsActuatorRequest(context.Request.Path.Value))
            {
                IEndpointOptions target = FindTargetEndpoint(context.Request.Path);
                if (target == null)
                {
                    await ReturnError(
                        context,
                        new SecurityResult(HttpStatusCode.ServiceUnavailable, ENDPOINT_NOT_CONFIGURED_MESSAGE));
                    return;
                }

                if (target.IsSensitive(_actuatorManagementOptions) && !HasSensitiveClaim(context))
                {
                    await ReturnError(
                        context,
                        new SecurityResult(HttpStatusCode.Unauthorized, ACCESS_DENIED_MESSAGE));

                    return;
                }
            }

            await Next.Invoke(context);
        }

        protected bool IsActuatorRequest(string path)
        {
            var contextPath = _actuatorManagementOptions.Path;
            return path.StartsWith(contextPath);
        }

        private async Task ReturnError(IOwinContext context, SecurityResult error)
        {
            LogError(context, error);
            context.Response.Headers.SetValues("Content-Type", new string[] { "application/json;charset=UTF-8" });
            context.Response.StatusCode = (int)error.Code;
            await context.Response.WriteAsync(Serialize(error));
        }

        private void LogError(IOwinContext context, SecurityResult error)
        {
            _logger?.LogError("Actuator Security Error: {ErrorCode} - {ErrorMessage}", error.Code, error.Message);
            if (_logger?.IsEnabled(LogLevel.Trace) == true)
            {
                foreach (var header in context.Request.Headers)
                {
                    _logger?.LogTrace("Header: {HeaderKey} - {HeaderValue}", header.Key, header.Value);
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

        private bool HasSensitiveClaim(IOwinContext context)
        {
            var claim = _actuatorManagementOptions.SensitiveClaim;
            var user = context.Authentication.User;
            return user != null && claim != null && user.HasClaim(claim.Type, claim.Value);
        }

        private IEndpointOptions FindTargetEndpoint(PathString path)
        {
            var configEndpoints = _actuatorManagementOptions.EndpointOptions;

            foreach (var ep in configEndpoints)
            {
                var contextPath = _actuatorManagementOptions.Path;

                if (!contextPath.EndsWith("/") && !string.IsNullOrEmpty(ep.Path))
                {
                    contextPath += "/";
                }

                var fullPath = contextPath + ep.Path;
                if (path.Value.Equals(fullPath) || (!string.IsNullOrEmpty(ep.Path) && path.StartsWithSegments(new PathString(fullPath))))
                {
                    return ep;
                }
            }

            return null;
        }
    }
}
