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
using Newtonsoft.Json;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.CloudFoundry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Discovery
{
    public class ActuatorSecurityMiddleware
    {
        protected readonly string ENDPOINT_NOT_CONFIGURED_MESSAGE = "Endpoint is not available";
        protected readonly string ACCESS_DENIED_MESSAGE = "Access denied";

        private RequestDelegate _next;
        private ILogger<ActuatorSecurityMiddleware> _logger;
        private IActuatorDiscoveryOptions _options;
        private IManagementOptions _actuatorManagementOptions;

        public ActuatorSecurityMiddleware(RequestDelegate next, IActuatorDiscoveryOptions options, IEnumerable<IManagementOptions> mgmtOptions, ILogger<ActuatorSecurityMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _options = options;
            _actuatorManagementOptions = mgmtOptions.OfType<ActuatorManagementOptions>().SingleOrDefault();
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("Invoke({0})", context.Request.Path.Value);
            if (IsActuatorRequest(context.Request.Path))
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

            await _next(context);
        }

        protected bool IsActuatorRequest(string path)
        {
            var contextPath = _actuatorManagementOptions.Path;
            return path.StartsWith(contextPath);
        }

        protected async Task ReturnError(HttpContext context, SecurityResult error)
        {
            LogError(context, error);
            context.Response.Headers.Add("Content-Type", "application/json;charset=UTF-8");
            context.Response.StatusCode = (int)error.Code;
            await context.Response.WriteAsync(Serialize(error));
        }

        protected string Serialize(SecurityResult error)
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

        protected void LogError(HttpContext context, SecurityResult error)
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

        private bool HasSensitiveClaim(HttpContext context)
        {
            var claim = _actuatorManagementOptions.SensitiveClaim;
            var user = context.User;
            return user.Identity.IsAuthenticated && claim != null && user.HasClaim(claim.Type, claim.Value);
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
                if (path.StartsWithSegments(new PathString(fullPath)))
                {
                    return ep;
                }
            }

            return null;
        }
    }
}
