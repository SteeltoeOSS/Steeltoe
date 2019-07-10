﻿// Copyright 2017 the original author or authors.
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

using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointOwin.CloudFoundry
{
    public class CloudFoundrySecurityOwinMiddleware : OwinMiddleware
    {
        private readonly ILogger<CloudFoundrySecurityOwinMiddleware> _logger;
        private readonly ICloudFoundryOptions _options;
        private readonly SecurityBase _base;
        private readonly IManagementOptions _mgmtOptions;

        public CloudFoundrySecurityOwinMiddleware(OwinMiddleware next, ICloudFoundryOptions options, IEnumerable<IManagementOptions> mgmtOptions, ILogger<CloudFoundrySecurityOwinMiddleware> logger = null)
            : base(next)
        {
            _options = options;
            _logger = logger;
            _mgmtOptions = mgmtOptions.OfType<CloudFoundryManagementOptions>().Single();
            _base = new SecurityBase(options, _mgmtOptions, logger);
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public CloudFoundrySecurityOwinMiddleware(OwinMiddleware next, ICloudFoundryOptions options, ILogger<CloudFoundrySecurityOwinMiddleware> logger = null)
            : base(next)
        {
            _options = options;
            _logger = logger;
            _base = new SecurityBase(options, logger);
        }

        public override async Task Invoke(IOwinContext context)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            bool isEnabled = _mgmtOptions == null ? _options.IsEnabled : _options.IsEnabled(_mgmtOptions);
#pragma warning restore CS0618 // Type or member is obsolete

            // if running on Cloud Foundry, security is enabled, the path starts with /cloudfoundryapplication...
            if (Platform.IsCloudFoundry && isEnabled && _base.IsCloudFoundryRequest(context.Request.Path.ToString()))
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
                    await ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, _base.APPLICATION_ID_MISSING_MESSAGE)).ConfigureAwait(false);
                    return;
                }

                // make sure we know where to get user permissions
                if (string.IsNullOrEmpty(_options.CloudFoundryApi))
                {
                    await ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, _base.CLOUDFOUNDRY_API_MISSING_MESSAGE)).ConfigureAwait(false);
                    return;
                }

                _logger?.LogTrace("Identifying which endpoint the request at {EndpointRequestPath} is for", context.Request.Path);
                IEndpointOptions target = FindTargetEndpoint(context.Request.Path);
                if (target == null)
                {
                    await ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, _base.ENDPOINT_NOT_CONFIGURED_MESSAGE)).ConfigureAwait(false);
                    return;
                }

                _logger?.LogTrace("Getting user permissions");
                var sr = await GetPermissions(context).ConfigureAwait(false);
                if (sr.Code != HttpStatusCode.OK)
                {
                    await ReturnError(context, sr).ConfigureAwait(false);
                    return;
                }

                _logger?.LogTrace("Applying user permissions to request");
                var permissions = sr.Permissions;
                if (!target.IsAccessAllowed(permissions))
                {
                    await ReturnError(context, new SecurityResult(HttpStatusCode.Forbidden, _base.ACCESS_DENIED_MESSAGE)).ConfigureAwait(false);
                    return;
                }

                _logger?.LogTrace("Access granted!");
            }

            await Next.Invoke(context).ConfigureAwait(false);
        }

        internal async Task<SecurityResult> GetPermissions(IOwinContext context)
        {
            string token = GetAccessToken(context.Request);
            return await _base.GetPermissionsAsync(token).ConfigureAwait(false);
        }

        internal string GetAccessToken(IOwinRequest request)
        {
            if (request.Headers.TryGetValue(_base.AUTHORIZATION_HEADER, out string[] headerVal))
            {
                string header = headerVal[0];
                if (header?.StartsWith(_base.BEARER, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return header.Substring(_base.BEARER.Length + 1);
                }
            }

            return null;
        }

        private IEndpointOptions FindTargetEndpoint(PathString path)
        {
            List<IEndpointOptions> configEndpoints;

            // Remove in 3.0
            if (_mgmtOptions == null)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                configEndpoints = _options.Global.EndpointOptions;
#pragma warning restore CS0618 // Type or member is obsolete
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

        private async Task ReturnError(IOwinContext context, SecurityResult error)
        {
            LogError(context, error);
            context.Response.Headers.SetValues("Content-Type", new string[] { "application/json;charset=UTF-8" });
            context.Response.StatusCode = (int)error.Code;
            await context.Response.WriteAsync(_base.Serialize(error)).ConfigureAwait(false);
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
    }
}
