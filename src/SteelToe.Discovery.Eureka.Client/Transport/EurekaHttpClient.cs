//
// Copyright 2015 the original author or authors.
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
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;

using SteelToe.Discovery.Eureka.AppInfo;
using Newtonsoft.Json;
using System.Text;
using SteelToe.Discovery.Eureka.Util;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Net.Security;
using System.Net;
using System.Runtime.InteropServices;

namespace SteelToe.Discovery.Eureka.Transport
{
    public class EurekaHttpClient : IEurekaHttpClient
    {
        protected string _serviceUrl;
        protected IDictionary<string, string> _headers;
        protected IEurekaClientConfig _config;

        protected HttpClient _client;
        protected ILogger _logger;

        protected EurekaHttpClient()
        {
        }

        public EurekaHttpClient(IEurekaClientConfig config, HttpClient client, ILoggerFactory logFactory = null) :
            this(config, new Dictionary<string, string>(), logFactory)
        {
            _client = client;
        }

        public EurekaHttpClient(IEurekaClientConfig config, ILoggerFactory logFactory = null) :
            this(config, new Dictionary<string, string>(), logFactory)
        {
        }

        public EurekaHttpClient(IEurekaClientConfig config, IDictionary<string, string> headers, ILoggerFactory logFactory = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }
            _logger = logFactory?.CreateLogger<EurekaHttpClient>();
            _config = config;
            _serviceUrl = MakeServiceUrl(config.EurekaServerServiceUrls);
            _headers = headers;
        }

        public virtual async Task<EurekaHttpResponse> RegisterAsync(InstanceInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            HttpClient client = GetHttpClient(_config);
            var requestUri = GetRequestUri(_serviceUrl + "apps/" + info.AppName);
            var request = GetRequestMessage(HttpMethod.Post, requestUri);
#if NET451
            // If certificate validation is disabled, inject a callback to handle properly
            RemoteCertificateValidationCallback prevValidator = null;
            if (!_config.ValidateCertificates)
            {
                prevValidator = ServicePointManager.ServerCertificateValidationCallback;
                ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            }
#endif
            try
            {
                request.Content = GetRequestContent(new JsonInstanceInfoRoot(info.ToJsonInstance()));

                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    _logger?.LogDebug("RegisterAsync {0}, status: {1}", requestUri.ToString(), response.StatusCode);
                    EurekaHttpResponse resp = new EurekaHttpResponse(response.StatusCode);
                    resp.Headers = response.Headers;
                    return resp;
                }

            }
            catch (Exception e)
            {
                _logger?.LogError("RegisterAsync Exception:", e);
                throw;
            }
#if NET451
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback = prevValidator;
            }
#endif
        }

        public virtual async Task<EurekaHttpResponse<InstanceInfo>> SendHeartBeatAsync(string appName, string id, InstanceInfo info, InstanceStatus overriddenStatus)
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
                {  "lastDirtyTimestamp", DateTimeConversions.ToJavaMillis(new DateTime(info.LastDirtyTimestamp, DateTimeKind.Utc)).ToString() }

            };

            if (overriddenStatus != InstanceStatus.UNKNOWN)
            {
                queryArgs.Add("overriddenstatus", overriddenStatus.ToString());
            }

            HttpClient client = GetHttpClient(_config);
            var requestUri = GetRequestUri(_serviceUrl + "apps/" + info.AppName + "/" + id, queryArgs);
            var request = GetRequestMessage(HttpMethod.Put, requestUri);
#if NET451
            // If certificate validation is disabled, inject a callback to handle properly
            RemoteCertificateValidationCallback prevValidator = null;
            if (!_config.ValidateCertificates)
            {
                prevValidator = ServicePointManager.ServerCertificateValidationCallback;
                ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            }
#endif
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

                    _logger?.LogDebug("SendHeartbeatAsync {0}, status: {1}, instanceInfo: {2}",
                        requestUri.ToString(), response.StatusCode, ((infoResp != null) ? infoResp.ToString() : "null"));
                    EurekaHttpResponse<InstanceInfo> resp = new EurekaHttpResponse<InstanceInfo>(response.StatusCode, infoResp);
                    resp.Headers = response.Headers;
                    return resp;
                }

            }
            catch (Exception e)
            {
                _logger?.LogError("SendHeartbeatAsync Exception: {0}", e);
                throw;
            }
#if NET451
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback = prevValidator;
            }
#endif
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

            HttpClient client = GetHttpClient(_config);
            var requestUri = GetRequestUri(_serviceUrl + "apps/" + appName);
            var request = GetRequestMessage(HttpMethod.Get, requestUri);
#if NET451
            // If certificate validation is disabled, inject a callback to handle properly
            RemoteCertificateValidationCallback prevValidator = null;
            if (!_config.ValidateCertificates)
            {
                prevValidator = ServicePointManager.ServerCertificateValidationCallback;
                ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            }
#endif
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
                    _logger?.LogDebug("GetApplicationAsync {0}, status: {1}, application: {2}",
                        requestUri.ToString(), response.StatusCode, ((appResp != null) ? appResp.ToString() : "null"));
                    EurekaHttpResponse<Application> resp = new EurekaHttpResponse<Application>(response.StatusCode, appResp);
                    resp.Headers = response.Headers;
                    return resp;
                }

            }
            catch (Exception e)
            {
                _logger?.LogError("GetApplicationAsync Exception: {0}", e);
                throw;
            }
#if NET451
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback = prevValidator;
            }
#endif

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

            HttpClient client = GetHttpClient(_config);
            var requestUri = GetRequestUri(_serviceUrl + "apps/" + appName + "/" + id);
            var request = GetRequestMessage(HttpMethod.Delete, requestUri);
#if NET451
            // If certificate validation is disabled, inject a callback to handle properly
            RemoteCertificateValidationCallback prevValidator = null;
            if (!_config.ValidateCertificates)
            {
                prevValidator = ServicePointManager.ServerCertificateValidationCallback;
                ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            }
#endif
            try
            {
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    _logger?.LogDebug("CancelAsync {0}, status: {1}", requestUri.ToString(), response.StatusCode);
                    EurekaHttpResponse resp = new EurekaHttpResponse(response.StatusCode);
                    resp.Headers = response.Headers;
                    return resp;
                }

            }
            catch (Exception e)
            {
                _logger?.LogError("CancelAsync Exception: {0}", e);
                throw;
            }
#if NET451
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback = prevValidator;
            }
#endif
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
                {  "lastDirtyTimestamp", DateTimeConversions.ToJavaMillis(new DateTime(info.LastDirtyTimestamp, DateTimeKind.Utc)).ToString() }
            };

            HttpClient client = GetHttpClient(_config);
            var requestUri = GetRequestUri(_serviceUrl + "apps/" + appName + "/" + id + "/status", queryArgs);
            var request = GetRequestMessage(HttpMethod.Delete, requestUri);
#if NET451
            // If certificate validation is disabled, inject a callback to handle properly
            RemoteCertificateValidationCallback prevValidator = null;
            if (!_config.ValidateCertificates)
            {
                prevValidator = ServicePointManager.ServerCertificateValidationCallback;
                ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            }
#endif
            try
            {
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    _logger?.LogDebug("DeleteStatusOverrideAsync {0}, status: {1}", requestUri.ToString(), response.StatusCode);
                    EurekaHttpResponse resp = new EurekaHttpResponse(response.StatusCode);
                    resp.Headers = response.Headers;
                    return resp;
                }

            }
            catch (Exception e)
            {
                _logger?.LogError("DeleteStatusOverrideAsync Exception: {0}", e);
                throw;
            }
#if NET451
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback = prevValidator;
            }
#endif
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
                {  "lastDirtyTimestamp", DateTimeConversions.ToJavaMillis(new DateTime(info.LastDirtyTimestamp, DateTimeKind.Utc)).ToString() }

            };

            HttpClient client = GetHttpClient(_config);
            var requestUri = GetRequestUri(_serviceUrl + "apps/" + appName + "/" + id + "/status", queryArgs);
            var request = GetRequestMessage(HttpMethod.Put, requestUri);
#if NET451
            // If certificate validation is disabled, inject a callback to handle properly
            RemoteCertificateValidationCallback prevValidator = null;
            if (!_config.ValidateCertificates)
            {
                prevValidator = ServicePointManager.ServerCertificateValidationCallback;
                ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            }
#endif
            try
            {
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    _logger?.LogDebug("StatusUpdateAsync {0}, status: {1}", requestUri.ToString(), response.StatusCode);
                    EurekaHttpResponse resp = new EurekaHttpResponse(response.StatusCode);
                    resp.Headers = response.Headers;
                    return resp;
                }

            }
            catch (Exception e)
            {
                _logger?.LogError("StatusUpdateAsync Exception: {0}", e);
                throw;
            }
#if NET451
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback = prevValidator;
            }
#endif
        }
        protected virtual async Task<EurekaHttpResponse<InstanceInfo>> DoGetInstanceAsync(string path)
        {


            var requestUri = GetRequestUri(_serviceUrl + path);
            var request = GetRequestMessage(HttpMethod.Get, requestUri);
            HttpClient client = GetHttpClient(_config);
#if NET451
            // If certificate validation is disabled, inject a callback to handle properly
            RemoteCertificateValidationCallback prevValidator = null;
            if (!_config.ValidateCertificates)
            {
                prevValidator = ServicePointManager.ServerCertificateValidationCallback;
                ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            }
#endif
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
                    _logger?.LogDebug("DoGetInstanceAsync {0}, status: {1}, instanceInfo: {2}",
                       requestUri.ToString(), response.StatusCode, ((infoResp != null) ? infoResp.ToString() : "null"));
                    EurekaHttpResponse<InstanceInfo> resp = new EurekaHttpResponse<InstanceInfo>(response.StatusCode, infoResp);
                    resp.Headers = response.Headers;
                    return resp;
                }

            }
            catch (Exception e)
            {
                _logger?.LogError("DoGetInstanceAsync Exception: {0}", e);
                throw;
            }
#if NET451
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback = prevValidator;
            }
#endif
        }

        protected virtual async Task<EurekaHttpResponse<Applications>> DoGetApplicationsAsync(string path, ISet<string> regions)
        {
            string regionParams = CommaDelimit(regions);

            var queryArgs = new Dictionary<string, string>();
            if (regionParams != null)
            {
                queryArgs.Add("regions", regionParams);
            }

            HttpClient client = GetHttpClient(_config);
            var requestUri = GetRequestUri(_serviceUrl + path, queryArgs);
            var request = GetRequestMessage(HttpMethod.Get, requestUri);
#if NET451
            // If certificate validation is disabled, inject a callback to handle properly
            RemoteCertificateValidationCallback prevValidator = null;
            if (!_config.ValidateCertificates)
            {
                prevValidator = ServicePointManager.ServerCertificateValidationCallback;
                ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            }
#endif
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
                    _logger?.LogDebug("DoGetApplicationsAsync {0}, status: {1}, applications: {2}",
                        requestUri.ToString(), response.StatusCode, ((appsResp != null) ? appsResp.ToString() : "null"));
                    EurekaHttpResponse<Applications> resp = new EurekaHttpResponse<Applications>(response.StatusCode, appsResp);
                    resp.Headers = response.Headers;
                    return resp;
                }

            }
            catch (Exception e)
            {
                _logger?.LogError("DoGetApplicationsAsync Exception: {0}", e);
                throw;
            }
#if NET451
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback = prevValidator;
            }
#endif
        }



        public virtual void Shutdown()
        {

        }

        protected virtual HttpClient GetHttpClient(IEurekaClientConfig config)
        {
            if (_client != null)
            {
                return _client;
            }

            HttpClient client = null;
#if NET451
            client = new HttpClient();
#else
            // TODO: For coreclr, disabling certificate validation only works on windows platform
            // https://github.com/dotnet/corefx/issues/4476
            if (config != null && !config.ValidateCertificates && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var handler = new WinHttpHandler();
                handler.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                return new HttpClient(handler);
            } else
            {
                client = new HttpClient();
            }
#endif

            if (config != null)
            {
                client.Timeout = new TimeSpan(0, 0, config.EurekaServerConnectTimeoutSeconds);
            }

            return client;

        }

        protected internal virtual HttpRequestMessage GetRequestMessage(HttpMethod method, Uri requestUri)
        {
            var request = new HttpRequestMessage(method, requestUri);
            foreach (var header in _headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
            request.Headers.Add("Accept", "application/json");
            return request;
        }

        protected virtual HttpContent GetRequestContent(object toSerialize)
        {
            try
            {
                string json = JsonConvert.SerializeObject(toSerialize);
                _logger?.LogDebug("GetRequestContent generated JSON: {0}", json);
                return new StringContent(json, Encoding.UTF8, "application/json");

            }
            catch (Exception e)
            {
                _logger?.LogError("GetRequestContent Exception: {0}", e);
            }

            return new StringContent(string.Empty, Encoding.UTF8, "application/json");
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

        protected internal static string MakeServiceUrl(string serviceUrl)
        {
            var url = new Uri(serviceUrl).ToString();
            if (url[url.Length - 1] != '/')
                url = url + '/';
            return url;
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
    }
}
