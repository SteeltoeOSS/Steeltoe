// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
        private readonly IManagementOptions _managementOptions;
        private readonly ILogger<CloudFoundrySecurity> _logger;
        private readonly ICloudFoundryOptions _options;
        private readonly SecurityBase _base;

        public CloudFoundrySecurity(ICloudFoundryOptions options, IManagementOptions managementOptions, ILogger<CloudFoundrySecurity> logger = null)
        {
            _options = options;
            _managementOptions = managementOptions;
            _logger = logger;
            _base = new SecurityBase(options, managementOptions, logger);
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public CloudFoundrySecurity(ICloudFoundryOptions options, ILogger<CloudFoundrySecurity> logger = null)
        {
            _options = options;
            _logger = logger;
            _base = new SecurityBase(options, logger);
        }

        public async Task<bool> IsAccessAllowed(HttpContextBase context, IEndpointOptions target)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var isEndpointEnabled = _managementOptions == null ? _options.IsEnabled : _options.IsEnabled(_managementOptions);
#pragma warning restore CS0618 // Type or member is obsolete
            var isEndpointExposed = _managementOptions == null || _options.IsExposed(_managementOptions);

            // if running on Cloud Foundry, security is enabled, the path starts with /cloudfoundryapplication...
            if (Platform.IsCloudFoundry && isEndpointEnabled && isEndpointExposed && _base.IsCloudFoundryRequest(context.Request.Path))
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
                    await ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, _base.APPLICATION_ID_MISSING_MESSAGE)).ConfigureAwait(false);
                    return false;
                }

                // make sure we know where to get user permissions
                if (string.IsNullOrEmpty(_options.CloudFoundryApi))
                {
                    await ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, _base.CLOUDFOUNDRY_API_MISSING_MESSAGE)).ConfigureAwait(false);
                    return false;
                }

                _logger?.LogTrace("Getting user permissions");
                var sr = await GetPermissions(context).ConfigureAwait(false);
                if (sr.Code != HttpStatusCode.OK)
                {
                    await ReturnError(context, sr).ConfigureAwait(false);
                    return false;
                }

                _logger?.LogTrace("Applying user permissions to request");
                var permissions = sr.Permissions;
                if (!target.IsAccessAllowed(permissions))
                {
                    await ReturnError(context, new SecurityResult(HttpStatusCode.Forbidden, _base.ACCESS_DENIED_MESSAGE)).ConfigureAwait(false);
                    return false;
                }

                _logger?.LogTrace("Access granted!");
            }

            return true;
        }

        internal async Task<SecurityResult> GetPermissions(HttpContextBase context)
        {
            var token = GetAccessToken(context.Request);
            return await _base.GetPermissionsAsync(token).ConfigureAwait(false);
        }

        internal string GetAccessToken(HttpRequestBase request)
        {
            var headerVals = request.Headers.GetValues(_base.AUTHORIZATION_HEADER);
            if (headerVals != null && headerVals.Length > 0)
            {
                var header = headerVals[0];
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

            // allowing override of 400-level errors is more likely to cause confusion than to be useful
            if (_managementOptions.UseStatusCodeFromResponse || (int)error.Code < 500)
            {
                context.Response.StatusCode = (int)error.Code;
            }

            await context.Response.Output.WriteAsync(_base.Serialize(error)).ConfigureAwait(false);
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
