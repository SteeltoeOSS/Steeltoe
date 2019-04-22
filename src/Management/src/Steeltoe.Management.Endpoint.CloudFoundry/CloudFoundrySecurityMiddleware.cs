//
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
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    public class CloudFoundrySecurityMiddleware
    {
        private RequestDelegate _next;
        private ILogger<CloudFoundrySecurityMiddleware> _logger;
        private ICloudFoundryOptions _options;

        private const string APPLICATION_ID_MISSING_MESSAGE = "Application id is not available";
        private const string ENDPOINT_NOT_CONFIGURED_MESSAGE = "Endpoint is not available";
        private const string AUTHORIZATION_HEADER_INVALID = "Authorization header is missing or invalid";
        private const string CLOUDFOUNDRY_API_MISSING_MESSAGE = "Cloud controller URL is not available";
        private const string CLOUDFOUNDRY_NOT_REACHABLE_MESSAGE = "Cloud controller not reachable";
        private const string ACCESS_DENIED_MESSAGE = "Access denied";
        //private const string UNABLE_TO_READ_TOKEN = "Unable to read token";
        private const string AUTHORIZATION_HEADER = "Authorization";
        private const string BEARER = "bearer";
        private const string READ_SENSITIVE_DATA = "read_sensitive_data";

        public CloudFoundrySecurityMiddleware(RequestDelegate next, ICloudFoundryOptions options, ILogger<CloudFoundrySecurityMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _options = options;
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("Invoke({0})", context.Request.Path.Value);

            if (IsCloudFoundryRequest(context))
            {
                if (string.IsNullOrEmpty(_options.ApplicationId))
                {
                    await ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, APPLICATION_ID_MISSING_MESSAGE));
                    return;
                }

                if (string.IsNullOrEmpty(_options.CloudFoundryApi))
                {
                    await ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, CLOUDFOUNDRY_API_MISSING_MESSAGE));
                    return;
                }

                IEndpointOptions target = FindTargetEndpoint(context.Request.Path);
                if (target == null)
                {
                    await ReturnError(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, ENDPOINT_NOT_CONFIGURED_MESSAGE));
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
                    await ReturnError(context, new SecurityResult(HttpStatusCode.Forbidden, ACCESS_DENIED_MESSAGE));
                    return;
                }
            }
       
            await _next(context);
        }

        private IEndpointOptions FindTargetEndpoint(PathString path)
        {
            var configEndpoints = this._options.Global.EndpointOptions;
            foreach(var ep in configEndpoints)
            {
                PathString epPath = new PathString(ep.Path);
                if (path.StartsWithSegments(epPath))
                {
                    return ep;
                }
            }
            return null;
        }

        private async Task<SecurityResult> GetPermissions(HttpContext context)
        {
            string token = GetAccessToken(context.Request);
            if (string.IsNullOrEmpty(token))
            {
                return new SecurityResult(HttpStatusCode.Unauthorized, AUTHORIZATION_HEADER_INVALID);
            }

            string checkPermissionsUri = _options.CloudFoundryApi + "/v2/apps/" + _options.ApplicationId + "/permissions";
            var request = new HttpRequestMessage(HttpMethod.Get, checkPermissionsUri);
            AuthenticationHeaderValue auth = new AuthenticationHeaderValue("bearer", token);
            request.Headers.Authorization = auth;

            try
            {
                _logger.LogDebug("GetPermissions({0}, {1})", checkPermissionsUri, token);

                using (var client = new HttpClient())
                {
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            _logger?.LogInformation("Cloud Foundry returned status: {0} while obtaining permissions from: {1}",
                                response.StatusCode, checkPermissionsUri);

                            if (response.StatusCode == HttpStatusCode.Forbidden)
                            {
                                return new SecurityResult(HttpStatusCode.Forbidden, ACCESS_DENIED_MESSAGE);
                            } else
                            {
                                return new SecurityResult(HttpStatusCode.ServiceUnavailable, CLOUDFOUNDRY_NOT_REACHABLE_MESSAGE);
                            }
                            
                        }
                        return new SecurityResult(await GetPermissions(response));
                        
                    }
                }
            } catch (Exception e)
            {
                _logger?.LogError("Cloud Foundry returned execption: {0} while obtaining permissions from: {1}",
                        e, checkPermissionsUri);
                return new SecurityResult(HttpStatusCode.ServiceUnavailable, CLOUDFOUNDRY_NOT_REACHABLE_MESSAGE);
            }

        }


        private async Task ReturnError(HttpContext context, SecurityResult error)
        {

            LogError(context, error);
            context.Response.Headers.Add("Content-Type", "application/json;charset=UTF-8");
            context.Response.StatusCode = (int)error.Code;
            await context.Response.WriteAsync(Serialize(error));

        }

        private string GetAccessToken(HttpRequest request)
        {
            StringValues headerVal ;
            if (request.Headers.TryGetValue(AUTHORIZATION_HEADER, out headerVal))
            {
                string header = headerVal.ToString();
                if (header.StartsWith(BEARER, StringComparison.OrdinalIgnoreCase))
                {
                    return header.Substring(BEARER.Length +1);
                }
            }
            return null;
        }

        private bool IsCloudFoundryRequest(HttpContext context)
        {
            PathString path = new PathString(_options.Path);
            return context.Request.Path.StartsWithSegments(path);
        }

        private string Serialize(SecurityResult error)
        {
            try
            {
                return JsonConvert.SerializeObject(error);
            }
            catch (Exception e)
            {
                _logger.LogError("Serialization Exception: {0}", e);
            }
            return string.Empty;
        }


        private async Task<Permissions> GetPermissions(HttpResponseMessage response)
        {
            string json = string.Empty;
            Permissions permissions = Permissions.NONE;

            try
            {
                json = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("GetPermisions returned json: {0}", json);

                var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                object perm;
                if (result.TryGetValue(READ_SENSITIVE_DATA, out perm))
                {
                    bool boolResult = (bool)perm;
                    if (boolResult)
                        permissions = Permissions.FULL;
                    else
                        permissions = Permissions.RESTRICTED;
                }
          

            } catch(Exception e)
            {
                _logger.LogError("Exception {0} extracting permissions from {1}", e, json);
            }

            _logger.LogDebug("GetPermisions returning: {0}", permissions);
            return permissions;
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

    class SecurityResult
    {
        public SecurityResult(Permissions level)
        {
            Code = HttpStatusCode.OK;
            Message = string.Empty;
            Permissions = level;
        }

        public SecurityResult(HttpStatusCode code, string message)
        {
            Code = code;
            Message = message;
            Permissions = Permissions.NONE;
        }

        [JsonIgnore]
        public HttpStatusCode Code;

        [JsonIgnore]
        public Permissions Permissions;

        [JsonProperty("security_error")]
        public string Message;

    }
}
