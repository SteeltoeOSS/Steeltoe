// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Steeltoe.Common;
using Steeltoe.Common.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    public class SecurityBase
    {
        public readonly int DEFAULT_GETPERMISSIONS_TIMEOUT = 5000;   // Milliseconds
        public readonly string APPLICATION_ID_MISSING_MESSAGE = "Application id is not available";
        public readonly string ENDPOINT_NOT_CONFIGURED_MESSAGE = "Endpoint is not available";
        public readonly string AUTHORIZATION_HEADER_INVALID = "Authorization header is missing or invalid";
        public readonly string CLOUDFOUNDRY_API_MISSING_MESSAGE = "Cloud controller URL is not available";
        public readonly string CLOUDFOUNDRY_NOT_REACHABLE_MESSAGE = "Cloud controller not reachable";
        public readonly string ACCESS_DENIED_MESSAGE = "Access denied";
        public readonly string AUTHORIZATION_HEADER = "Authorization";
        public readonly string BEARER = "bearer";
        public readonly string READ_SENSITIVE_DATA = "read_sensitive_data";
        private readonly ICloudFoundryOptions _options;
        private readonly IManagementOptions _mgmtOptions;
        private readonly ILogger _logger;

        public SecurityBase(ICloudFoundryOptions options, IManagementOptions mgmtOptions, ILogger logger = null)
        {
            _options = options;
            _mgmtOptions = mgmtOptions;
            _logger = logger;
        }

        public bool IsCloudFoundryRequest(string requestPath)
        {
            var contextPath = _mgmtOptions == null ? _options.Path : _mgmtOptions.Path;
            return requestPath.StartsWith(contextPath, StringComparison.InvariantCultureIgnoreCase);
        }

        public string Serialize(SecurityResult error)
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

        public async Task<SecurityResult> GetPermissionsAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return new SecurityResult(HttpStatusCode.Unauthorized, AUTHORIZATION_HEADER_INVALID);
            }

            string checkPermissionsUri = _options.CloudFoundryApi + "/v2/apps/" + _options.ApplicationId + "/permissions";
            var request = new HttpRequestMessage(HttpMethod.Get, checkPermissionsUri);
            AuthenticationHeaderValue auth = new AuthenticationHeaderValue("bearer", token);
            request.Headers.Authorization = auth;

            // If certificate validation is disabled, inject a callback to handle properly
            HttpClientHelper.ConfigureCertificateValidation(
                _options.ValidateCertificates,
                out SecurityProtocolType prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);
            try
            {
                _logger?.LogDebug("GetPermissions({0}, {1})", checkPermissionsUri, SecurityUtilities.SanitizeInput(token));

                // If certificate validation is disabled, inject a callback to handle properly
                HttpClientHelper.ConfigureCertificateValidation(
                    _options.ValidateCertificates,
                    out prevProtocols,
                    out prevValidator);
                using (var client = HttpClientHelper.GetHttpClient(_options.ValidateCertificates, DEFAULT_GETPERMISSIONS_TIMEOUT))
                {
                    using (HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            _logger?.LogInformation(
                                "Cloud Foundry returned status: {HttpStatus} while obtaining permissions from: {PermissionsUri}",
                                response.StatusCode,
                                checkPermissionsUri);

                            return response.StatusCode == HttpStatusCode.Forbidden
                                ? new SecurityResult(HttpStatusCode.Forbidden, ACCESS_DENIED_MESSAGE)
                                : new SecurityResult(HttpStatusCode.ServiceUnavailable, CLOUDFOUNDRY_NOT_REACHABLE_MESSAGE);
                        }

                        return new SecurityResult(await GetPermissions(response).ConfigureAwait(false));
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.LogError("Cloud Foundry returned exception: {SecurityException} while obtaining permissions from: {PermissionsUri}", e, checkPermissionsUri);
                return new SecurityResult(HttpStatusCode.ServiceUnavailable, CLOUDFOUNDRY_NOT_REACHABLE_MESSAGE);
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_options.ValidateCertificates, prevProtocols, prevValidator);
            }
        }

        public async Task<Permissions> GetPermissions(HttpResponseMessage response)
        {
            string json = string.Empty;
            Permissions permissions = Permissions.NONE;

            try
            {
                json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                _logger?.LogDebug("GetPermisions returned json: {0}", SecurityUtilities.SanitizeInput(json));

                var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                if (result.TryGetValue(READ_SENSITIVE_DATA, out object perm))
                {
                    bool boolResult = (bool)perm;
                    permissions = boolResult ? Permissions.FULL : Permissions.RESTRICTED;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError("Exception {0} extracting permissions from {1}", e, SecurityUtilities.SanitizeInput(json));
                throw;
            }

            _logger?.LogDebug("GetPermisions returning: {0}", permissions);
            return permissions;
        }
    }
}
