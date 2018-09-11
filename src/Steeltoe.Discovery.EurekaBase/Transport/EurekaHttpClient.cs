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
using Steeltoe.Common.Http;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Discovery.Eureka.Transport
{
    public class EurekaHttpClient : IEurekaHttpClient
    {
        protected string _serviceUrl;
        protected IDictionary<string, string> _headers;

        protected IEurekaClientConfig _config;

        protected virtual IEurekaClientConfig Config
        {
            get
            {
                return _config;
            }
        }

        protected HttpClient _client;
        protected ILogger _logger;
        private static readonly char[] COLON_DELIMIT = new char[] { ':' };

        public EurekaHttpClient(IEurekaClientConfig config, HttpClient client, ILoggerFactory logFactory = null)
            : this(config, new Dictionary<string, string>(), logFactory) => _client = client;

        public EurekaHttpClient(IEurekaClientConfig config, ILoggerFactory logFactory = null)
            : this(config, new Dictionary<string, string>(), logFactory)
        {
        }

        public EurekaHttpClient(IEurekaClientConfig config, IDictionary<string, string> headers, ILoggerFactory logFactory = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            Initialize(headers, logFactory);
        }

        protected EurekaHttpClient()
        {
        }

        public virtual async Task<EurekaHttpResponse> RegisterAsync(InstanceInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            HttpClient client = GetHttpClient(Config);
            var requestUri = GetRequestUri(_serviceUrl + "apps/" + info.AppName);
            var request = GetRequestMessage(HttpMethod.Post, requestUri);

            // If certificate validation is disabled, inject a callback to handle properly
            SecurityProtocolType prevProtocols = (SecurityProtocolType)0;
            HttpClientHelper.ConfigureCertificateValidatation(
                Config.ValidateCertificates,
                out prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);
            try
            {
                request.Content = GetRequestContent(new JsonInstanceInfoRoot(info.ToJsonInstance()));

                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    _logger?.LogDebug("RegisterAsync {RequestUri}, status: {StatusCode}", requestUri.ToString(), response.StatusCode);
                    EurekaHttpResponse resp = new EurekaHttpResponse(response.StatusCode)
                    {
                        Headers = response.Headers
                    };
                    if (response.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        var jsonError = await response.Content.ReadAsStringAsync();
                        _logger?.LogInformation($"Something goes wrong in registering: {jsonError}");
                    }

                    return resp;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "RegisterAsync Failed");
                throw;
            }
            finally
            {
                DisposeHttpClient(client);
                HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
            }
        }

        public virtual async Task<EurekaHttpResponse<InstanceInfo>> SendHeartBeatAsync(
            string appName,
            string id,
            InstanceInfo info,
            InstanceStatus overriddenStatus)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            if (string.IsNullOrEmpty(appName))
            {
                throw new ArgumentException(nameof(appName));
            }

            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }

            var queryArgs = new Dictionary<string, string>()
            {
                { "status", info.Status.ToString() },
                { "lastDirtyTimestamp", DateTimeConversions.ToJavaMillis(new DateTime(info.LastDirtyTimestamp, DateTimeKind.Utc)).ToString() }
            };

            if (overriddenStatus != InstanceStatus.UNKNOWN)
            {
                queryArgs.Add("overriddenstatus", overriddenStatus.ToString());
            }

            HttpClient client = GetHttpClient(Config);
            var requestUri = GetRequestUri(_serviceUrl + "apps/" + info.AppName + "/" + id, queryArgs);
            var request = GetRequestMessage(HttpMethod.Put, requestUri);

            // If certificate validation is disabled, inject a callback to handle properly
            SecurityProtocolType prevProtocols = (SecurityProtocolType)0;
            HttpClientHelper.ConfigureCertificateValidatation(
                Config.ValidateCertificates,
                out prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);
            try
            {
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    Stream stream = await response.Content.ReadAsStreamAsync();
                    JsonInstanceInfo jinfo = JsonInstanceInfo.Deserialize(stream);

                    InstanceInfo infoResp = null;
                    if (jinfo != null)
                    {
                        infoResp = InstanceInfo.FromJsonInstance(jinfo);
                    }

                    _logger?.LogDebug(
                        "SendHeartbeatAsync {RequestUri}, status: {StatusCode}, instanceInfo: {Instance}",
                        requestUri.ToString(),
                        response.StatusCode,
                        (infoResp != null) ? infoResp.ToString() : "null");
                    EurekaHttpResponse<InstanceInfo> resp = new EurekaHttpResponse<InstanceInfo>(response.StatusCode, infoResp)
                    {
                        Headers = response.Headers
                    };
                    return resp;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "SendHeartBeatAsync Failed");
                throw;
            }
            finally
            {
                DisposeHttpClient(client);
                HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
            }
        }

        public virtual async Task<EurekaHttpResponse<Applications>> GetApplicationsAsync(ISet<string> regions = null)
        {
            return await DoGetApplicationsAsync("apps/", regions);
        }

        public virtual async Task<EurekaHttpResponse<Applications>> GetDeltaAsync(ISet<string> regions = null)
        {
            return await DoGetApplicationsAsync("apps/delta", regions);
        }

        public virtual async Task<EurekaHttpResponse<Applications>> GetVipAsync(string vipAddress, ISet<string> regions = null)
        {
            if (string.IsNullOrEmpty(vipAddress))
            {
                throw new ArgumentException(nameof(vipAddress));
            }

            return await DoGetApplicationsAsync("vips/" + vipAddress, regions);
        }

        public virtual async Task<EurekaHttpResponse<Applications>> GetSecureVipAsync(string secureVipAddress, ISet<string> regions = null)
        {
            if (string.IsNullOrEmpty(secureVipAddress))
            {
                throw new ArgumentException(nameof(secureVipAddress));
            }

            return await DoGetApplicationsAsync("vips/" + secureVipAddress, regions);
        }

        public virtual async Task<EurekaHttpResponse<Application>> GetApplicationAsync(string appName)
        {
            if (string.IsNullOrEmpty(appName))
            {
                throw new ArgumentException(nameof(appName));
            }

            HttpClient client = GetHttpClient(Config);
            var requestUri = GetRequestUri(_serviceUrl + "apps/" + appName);
            var request = GetRequestMessage(HttpMethod.Get, requestUri);

            // If certificate validation is disabled, inject a callback to handle properly
            SecurityProtocolType prevProtocols = (SecurityProtocolType)0;
            HttpClientHelper.ConfigureCertificateValidatation(
                Config.ValidateCertificates,
                out prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);
            try
            {
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    Stream stream = await response.Content.ReadAsStreamAsync();
                    JsonApplicationRoot jroot = JsonApplicationRoot.Deserialize(stream);

                    Application appResp = null;
                    if (jroot != null)
                    {
                        appResp = Application.FromJsonApplication(jroot.Application);
                    }

                    _logger?.LogDebug(
                        "GetApplicationAsync {RequestUri}, status: {StatusCode}, application: {Application}",
                        requestUri.ToString(),
                        response.StatusCode,
                        (appResp != null) ? appResp.ToString() : "null");
                    EurekaHttpResponse<Application> resp = new EurekaHttpResponse<Application>(response.StatusCode, appResp)
                    {
                        Headers = response.Headers
                    };
                    return resp;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "GetApplicationAsync Failed");
                throw;
            }
            finally
            {
                DisposeHttpClient(client);
                HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
            }
        }

        public virtual async Task<EurekaHttpResponse<InstanceInfo>> GetInstanceAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }

            return await DoGetInstanceAsync("instances/" + id);
        }

        public virtual async Task<EurekaHttpResponse<InstanceInfo>> GetInstanceAsync(string appName, string id)
        {
            if (string.IsNullOrEmpty(appName))
            {
                throw new ArgumentException(nameof(appName));
            }

            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }

            return await DoGetInstanceAsync("apps/" + appName + "/" + id);
        }

        public virtual async Task<EurekaHttpResponse> CancelAsync(string appName, string id)
        {
            if (string.IsNullOrEmpty(appName))
            {
                throw new ArgumentException(nameof(appName));
            }

            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }

            HttpClient client = GetHttpClient(Config);
            var requestUri = GetRequestUri(_serviceUrl + "apps/" + appName + "/" + id);
            var request = GetRequestMessage(HttpMethod.Delete, requestUri);

            // If certificate validation is disabled, inject a callback to handle properly
            SecurityProtocolType prevProtocols = (SecurityProtocolType)0;
            HttpClientHelper.ConfigureCertificateValidatation(
                Config.ValidateCertificates,
                out prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);

            try
            {
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    _logger?.LogDebug("CancelAsync {RequestUri}, status: {StatusCode}", requestUri.ToString(), response.StatusCode);
                    EurekaHttpResponse resp = new EurekaHttpResponse(response.StatusCode)
                    {
                        Headers = response.Headers
                    };
                    return resp;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "CancelAsync Failed");
                throw;
            }
            finally
            {
                DisposeHttpClient(client);
                HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
            }
        }

        public virtual async Task<EurekaHttpResponse> DeleteStatusOverrideAsync(string appName, string id, InstanceInfo info)
        {
            if (string.IsNullOrEmpty(appName))
            {
                throw new ArgumentException(nameof(appName));
            }

            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }

            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            var queryArgs = new Dictionary<string, string>()
            {
                { "lastDirtyTimestamp", DateTimeConversions.ToJavaMillis(new DateTime(info.LastDirtyTimestamp, DateTimeKind.Utc)).ToString() }
            };

            HttpClient client = GetHttpClient(Config);
            var requestUri = GetRequestUri(_serviceUrl + "apps/" + appName + "/" + id + "/status", queryArgs);
            var request = GetRequestMessage(HttpMethod.Delete, requestUri);

            // If certificate validation is disabled, inject a callback to handle properly
            SecurityProtocolType prevProtocols = (SecurityProtocolType)0;
            HttpClientHelper.ConfigureCertificateValidatation(
                Config.ValidateCertificates,
                out prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);
            try
            {
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    _logger?.LogDebug("DeleteStatusOverrideAsync {RequestUri}, status: {StatusCode}", requestUri.ToString(), response.StatusCode);
                    EurekaHttpResponse resp = new EurekaHttpResponse(response.StatusCode)
                    {
                        Headers = response.Headers
                    };
                    return resp;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "DeleteStatusOverrideAsync Failed");
                throw;
            }
            finally
            {
                DisposeHttpClient(client);
                HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
            }
        }

        public virtual async Task<EurekaHttpResponse> StatusUpdateAsync(string appName, string id, InstanceStatus newStatus, InstanceInfo info)
        {
            if (string.IsNullOrEmpty(appName))
            {
                throw new ArgumentException(nameof(appName));
            }

            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }

            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            var queryArgs = new Dictionary<string, string>()
            {
                { "value", newStatus.ToString() },
                { "lastDirtyTimestamp", DateTimeConversions.ToJavaMillis(new DateTime(info.LastDirtyTimestamp, DateTimeKind.Utc)).ToString() }
            };

            HttpClient client = GetHttpClient(Config);
            var requestUri = GetRequestUri(_serviceUrl + "apps/" + appName + "/" + id + "/status", queryArgs);
            var request = GetRequestMessage(HttpMethod.Put, requestUri);

            // If certificate validation is disabled, inject a callback to handle properly
            SecurityProtocolType prevProtocols = (SecurityProtocolType)0;
            HttpClientHelper.ConfigureCertificateValidatation(
                Config.ValidateCertificates,
                out prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);

            try
            {
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    _logger?.LogDebug("StatusUpdateAsync {RequestUri}, status: {StatusCode}", requestUri.ToString(), response.StatusCode);
                    EurekaHttpResponse resp = new EurekaHttpResponse(response.StatusCode)
                    {
                        Headers = response.Headers
                    };
                    return resp;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "StatusUpdateAsync Failed");
                throw;
            }
            finally
            {
                DisposeHttpClient(client);
                HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
            }
        }

        public virtual void Shutdown()
        {
        }

        protected internal static string MakeServiceUrl(string serviceUrl)
        {
            var url = new Uri(serviceUrl).ToString();
            if (url[url.Length - 1] != '/')
            {
                url = url + '/';
            }

            return url;
        }

        protected internal virtual HttpRequestMessage GetRequestMessage(HttpMethod method, Uri requestUri)
        {
            string rawUri = requestUri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
            string rawUserInfo = requestUri.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped);

            var request = new HttpRequestMessage(method, rawUri);
            if (!string.IsNullOrEmpty(rawUserInfo) && rawUserInfo.Contains(":"))
            {
                string[] userInfo = GetUserInfo(rawUserInfo);
                if (userInfo.Length >= 2)
                {
                    request = HttpClientHelper.GetRequestMessage(method, rawUri, userInfo[0], userInfo[1]);
                }
            }

            foreach (var header in _headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            request.Headers.Add("Accept", "application/json");
            return request;
        }

        protected internal virtual Uri GetRequestUri(string baseUri, IDictionary<string, string> queryValues = null)
        {
            string uri = baseUri;
            if (queryValues != null)
            {
                StringBuilder sb = new StringBuilder();
                string sep = "?";
                foreach (var kvp in queryValues)
                {
                    sb.Append(sep + kvp.Key + "=" + kvp.Value);
                    sep = "&";
                }

                uri = uri + sb.ToString();
            }

            return new Uri(uri);
        }

        protected void Initialize(IDictionary<string, string> headers, ILoggerFactory logFactory)
        {
            _logger = logFactory?.CreateLogger<EurekaHttpClient>();
            _serviceUrl = MakeServiceUrl(Config.EurekaServerServiceUrls);
            _headers = headers ?? throw new ArgumentNullException(nameof(headers));
        }

        protected virtual async Task<EurekaHttpResponse<InstanceInfo>> DoGetInstanceAsync(string path)
        {
            var requestUri = GetRequestUri(_serviceUrl + path);
            var request = GetRequestMessage(HttpMethod.Get, requestUri);
            HttpClient client = GetHttpClient(Config);

            // If certificate validation is disabled, inject a callback to handle properly
            SecurityProtocolType prevProtocols = (SecurityProtocolType)0;
            HttpClientHelper.ConfigureCertificateValidatation(
                Config.ValidateCertificates,
                out prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);

            try
            {
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    Stream stream = await response.Content.ReadAsStreamAsync();
                    JsonInstanceInfoRoot jroot = JsonInstanceInfoRoot.Deserialize(stream);

                    InstanceInfo infoResp = null;
                    if (jroot != null)
                    {
                        infoResp = InstanceInfo.FromJsonInstance(jroot.Instance);
                    }

                    _logger?.LogDebug(
                        "DoGetInstanceAsync {RequestUri}, status: {StatusCode}, instanceInfo: {Instance}",
                       requestUri.ToString(),
                       response.StatusCode,
                       (infoResp != null) ? infoResp.ToString() : "null");
                    EurekaHttpResponse<InstanceInfo> resp = new EurekaHttpResponse<InstanceInfo>(response.StatusCode, infoResp)
                    {
                        Headers = response.Headers
                    };
                    return resp;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "DoGetInstanceAsync Failed");
                throw;
            }
            finally
            {
                DisposeHttpClient(client);
                HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
            }
        }

        protected virtual async Task<EurekaHttpResponse<Applications>> DoGetApplicationsAsync(string path, ISet<string> regions)
        {
            string regionParams = CommaDelimit(regions);

            var queryArgs = new Dictionary<string, string>();
            if (regionParams != null)
            {
                queryArgs.Add("regions", regionParams);
            }

            HttpClient client = GetHttpClient(Config);
            var requestUri = GetRequestUri(_serviceUrl + path, queryArgs);
            var request = GetRequestMessage(HttpMethod.Get, requestUri);

            // If certificate validation is disabled, inject a callback to handle properly
            SecurityProtocolType prevProtocols = (SecurityProtocolType)0;
            HttpClientHelper.ConfigureCertificateValidatation(
                Config.ValidateCertificates,
                out prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);

            try
            {
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    Stream stream = await response.Content.ReadAsStreamAsync();
                    JsonApplicationsRoot jroot = JsonApplicationsRoot.Deserialize(stream);

                    Applications appsResp = null;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        if (jroot != null)
                        {
                            appsResp = Applications.FromJsonApplications(jroot.Applications);
                        }
                    }

                    _logger?.LogDebug(
                        "DoGetApplicationsAsync {RequestUri}, status: {StatusCode}, applications: {Application}",
                        requestUri.ToString(),
                        response.StatusCode,
                        (appsResp != null) ? appsResp.ToString() : "null");
                    EurekaHttpResponse<Applications> resp = new EurekaHttpResponse<Applications>(response.StatusCode, appsResp)
                    {
                        Headers = response.Headers
                    };
                    return resp;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "DoGetApplicationsAsync Failed");
                throw;
            }
            finally
            {
                DisposeHttpClient(client);
                HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
            }
        }

        protected virtual HttpClient GetHttpClient(IEurekaClientConfig config)
        {
            if (_client != null)
            {
                return _client;
            }

            return HttpClientHelper.GetHttpClient(config.ValidateCertificates, config.EurekaServerConnectTimeoutSeconds * 1000);
        }

        protected virtual HttpContent GetRequestContent(object toSerialize)
        {
            try
            {
                string json = JsonConvert.SerializeObject(toSerialize);
                _logger?.LogDebug($"GetRequestContent generated JSON: {json}");
                return new StringContent(json, Encoding.UTF8, "application/json");
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "GetRequestContent Failed");
            }

            return new StringContent(string.Empty, Encoding.UTF8, "application/json");
        }

        protected virtual void DisposeHttpClient(HttpClient client)
        {
            if (client == null)
            {
                return;
            }

            if (_client != client)
            {
                client.Dispose();
            }
        }

        private static string CommaDelimit(ICollection<string> toJoin)
        {
            if (toJoin == null || toJoin.Count == 0)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            string sep = string.Empty;
            foreach (var value in toJoin)
            {
                sb.Append(sep);
                sb.Append(value);
                sep = ",";
            }

            return sb.ToString();
        }

        private string[] GetUserInfo(string userInfo)
        {
            string[] result = null;
            if (!string.IsNullOrEmpty(userInfo))
            {
                result = userInfo.Split(COLON_DELIMIT);
            }

            return result;
        }
    }
}
