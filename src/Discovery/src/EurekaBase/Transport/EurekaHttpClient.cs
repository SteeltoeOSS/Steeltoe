// Copyright 2017 the original author or authors.
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
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.Http;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Discovery.Eureka.Transport
{
    public class EurekaHttpClient : IEurekaHttpClient
    {
        protected internal string _serviceUrl;

        protected object _lock = new object();
        protected IList<string> _failingServiceUrls = new List<string>();

        protected IDictionary<string, string> _headers;

        protected IEurekaClientConfig _config;
        protected IEurekaDiscoveryClientHandlerProvider _handlerProvider;

        private const int DEFAULT_NUMBER_OF_RETRIES = 3;
        private const string HTTP_X_DISCOVERY_ALLOW_REDIRECT = "X-Discovery-AllowRedirect";

        protected virtual IEurekaClientConfig Config
        {
            get
            {
                if (_configOptions != null)
                {
                    return _configOptions.CurrentValue;
                }

                return _config;
            }
        }

        protected HttpClient _client;
        protected ILogger _logger;
        private const int DEFAULT_GETACCESSTOKEN_TIMEOUT = 10000; // Milliseconds
        private static readonly char[] COLON_DELIMIT = new char[] { ':' };
        private IOptionsMonitor<EurekaClientOptions> _configOptions;

        public EurekaHttpClient(IOptionsMonitor<EurekaClientOptions> config, IEurekaDiscoveryClientHandlerProvider handlerProvider = null, ILoggerFactory logFactory = null)
        {
            _config = null;
            _configOptions = config ?? throw new ArgumentNullException(nameof(config));
            _handlerProvider = handlerProvider;
            Initialize(new Dictionary<string, string>(), logFactory);
        }

        public EurekaHttpClient(IEurekaClientConfig config, HttpClient client, ILoggerFactory logFactory = null)
            : this(config, new Dictionary<string, string>(), logFactory) => _client = client;

        public EurekaHttpClient(IEurekaClientConfig config, ILoggerFactory logFactory = null, IEurekaDiscoveryClientHandlerProvider handlerProvider = null)
            : this(config, new Dictionary<string, string>(), logFactory, handlerProvider)
        {
        }

        public EurekaHttpClient(IEurekaClientConfig config, IDictionary<string, string> headers, ILoggerFactory logFactory = null, IEurekaDiscoveryClientHandlerProvider handlerProvider = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _handlerProvider = handlerProvider;
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

            IList<string> candidateServiceUrls = GetServiceUrlCandidates();
            int indx = 0;
            string serviceUrl = null;

            // For Retrys
            for (int retry = 0; retry < GetRetryCount(Config); retry++)
            {
                HttpClient client = GetHttpClient(Config);

                // If certificate validation is disabled, inject a callback to handle properly
                SecurityProtocolType prevProtocols = (SecurityProtocolType)0;
                HttpClientHelper.ConfigureCertificateValidation(
                    Config.ValidateCertificates,
                    out prevProtocols,
                    out RemoteCertificateValidationCallback prevValidator);

                serviceUrl = GetServiceUrl(candidateServiceUrls, ref indx);
                var requestUri = GetRequestUri(serviceUrl + "apps/" + info.AppName);
                var request = GetRequestMessage(HttpMethod.Post, requestUri);

                try
                {
                    request.Content = GetRequestContent(new JsonInstanceInfoRoot(info.ToJsonInstance()));

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        _logger?.LogDebug("RegisterAsync {RequestUri}, status: {StatusCode}, retry: {retry}", requestUri.ToMaskedString(), response.StatusCode, retry);
                        int statusCode = (int)response.StatusCode;
                        if ((statusCode >= 200 && statusCode < 300) || statusCode == 404)
                        {
                            Interlocked.Exchange(ref _serviceUrl, serviceUrl);
                            EurekaHttpResponse resp = new EurekaHttpResponse(response.StatusCode)
                            {
                                Headers = response.Headers
                            };
                            return resp;
                        }

                        var jsonError = await response.Content.ReadAsStringAsync();
                        _logger?.LogInformation("Failure during RegisterAsync: {jsonError}", jsonError);
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "RegisterAsync Failed, request was made to {requestUri}, retry: {retry}", requestUri.ToMaskedUri(), retry);
                }
                finally
                {
                    DisposeHttpClient(client);
                    HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
                }

                Interlocked.CompareExchange(ref _serviceUrl, null, serviceUrl);
                AddToFailingServiceUrls(serviceUrl);
            }

            throw new EurekaTransportException("Retry limit reached; giving up on completing the RegisterAsync request");
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

            IList<string> candidateServiceUrls = GetServiceUrlCandidates();
            int indx = 0;
            string serviceUrl = null;

            for (int retry = 0; retry < GetRetryCount(Config); retry++)
            {
                HttpClient client = GetHttpClient(Config);

                // If certificate validation is disabled, inject a callback to handle properly
                SecurityProtocolType prevProtocols = (SecurityProtocolType)0;
                HttpClientHelper.ConfigureCertificateValidation(
                    Config.ValidateCertificates,
                    out prevProtocols,
                    out RemoteCertificateValidationCallback prevValidator);

                serviceUrl = GetServiceUrl(candidateServiceUrls, ref indx);
                var requestUri = GetRequestUri(serviceUrl + "apps/" + info.AppName + "/" + id, queryArgs);
                var request = GetRequestMessage(HttpMethod.Put, requestUri);

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
                            "SendHeartbeatAsync {RequestUri}, status: {StatusCode}, instanceInfo: {Instance}, retry: {retry}",
                            requestUri.ToMaskedString(),
                            response.StatusCode,
                            (infoResp != null) ? infoResp.ToString() : "null",
                            retry);
                        int statusCode = (int)response.StatusCode;
                        if ((statusCode >= 200 && statusCode < 300) || statusCode == 404)
                        {
                            Interlocked.Exchange(ref _serviceUrl, serviceUrl);
                            EurekaHttpResponse<InstanceInfo> resp = new EurekaHttpResponse<InstanceInfo>(response.StatusCode, infoResp)
                            {
                                Headers = response.Headers
                            };
                            return resp;
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "SendHeartBeatAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
                }
                finally
                {
                    DisposeHttpClient(client);
                    HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
                }

                Interlocked.CompareExchange(ref _serviceUrl, null, serviceUrl);
                AddToFailingServiceUrls(serviceUrl);
            }

            throw new EurekaTransportException("Retry limit reached; giving up on completing the SendHeartBeatAsync request");
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

            IList<string> candidateServiceUrls = GetServiceUrlCandidates();
            int indx = 0;
            string serviceUrl = null;

            // For Retrys
            for (int retry = 0; retry < GetRetryCount(Config); retry++)
            {
                HttpClient client = GetHttpClient(Config);

                // If certificate validation is disabled, inject a callback to handle properly
                SecurityProtocolType prevProtocols = (SecurityProtocolType)0;
                HttpClientHelper.ConfigureCertificateValidation(
                    Config.ValidateCertificates,
                    out prevProtocols,
                    out RemoteCertificateValidationCallback prevValidator);

                serviceUrl = GetServiceUrl(candidateServiceUrls, ref indx);
                var requestUri = GetRequestUri(serviceUrl + "apps/" + appName);
                var request = GetRequestMessage(HttpMethod.Get, requestUri);

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
                            "GetApplicationAsync {RequestUri}, status: {StatusCode}, application: {Application}, retry: {retry}",
                            requestUri.ToMaskedString(),
                            response.StatusCode,
                            (appResp != null) ? appResp.ToString() : "null",
                            retry);
                        int statusCode = (int)response.StatusCode;
                        if ((statusCode >= 200 && statusCode < 300) || statusCode == 403 || statusCode == 404)
                        {
                            Interlocked.Exchange(ref _serviceUrl, serviceUrl);
                            EurekaHttpResponse<Application> resp = new EurekaHttpResponse<Application>(response.StatusCode, appResp)
                            {
                                Headers = response.Headers
                            };
                            return resp;
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "GetApplicationAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
                }
                finally
                {
                    DisposeHttpClient(client);
                    HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
                }

                Interlocked.CompareExchange(ref _serviceUrl, null, serviceUrl);
                AddToFailingServiceUrls(serviceUrl);
            }

            throw new EurekaTransportException("Retry limit reached; giving up on completing the GetApplicationAsync request");
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

            IList<string> candidateServiceUrls = GetServiceUrlCandidates();
            int indx = 0;
            string serviceUrl = null;

            // For Retrys
            for (int retry = 0; retry < GetRetryCount(Config); retry++)
            {
                HttpClient client = GetHttpClient(Config);

                // If certificate validation is disabled, inject a callback to handle properly
                SecurityProtocolType prevProtocols = (SecurityProtocolType)0;
                HttpClientHelper.ConfigureCertificateValidation(
                    Config.ValidateCertificates,
                    out prevProtocols,
                    out RemoteCertificateValidationCallback prevValidator);

                serviceUrl = GetServiceUrl(candidateServiceUrls, ref indx);
                var requestUri = GetRequestUri(serviceUrl + "apps/" + appName + "/" + id);
                var request = GetRequestMessage(HttpMethod.Delete, requestUri);

                try
                {
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        _logger?.LogDebug("CancelAsync {RequestUri}, status: {StatusCode}, retry: {retry}", requestUri.ToMaskedString(), response.StatusCode, retry);
                        Interlocked.Exchange(ref _serviceUrl, serviceUrl);
                        EurekaHttpResponse resp = new EurekaHttpResponse(response.StatusCode)
                        {
                            Headers = response.Headers
                        };
                        return resp;
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "CancelAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
                }
                finally
                {
                    DisposeHttpClient(client);
                    HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
                }

                Interlocked.CompareExchange(ref _serviceUrl, null, serviceUrl);
                AddToFailingServiceUrls(serviceUrl);
            }

            throw new EurekaTransportException("Retry limit reached; giving up on completing the CancelAsync request");
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

            IList<string> candidateServiceUrls = GetServiceUrlCandidates();
            int indx = 0;
            string serviceUrl = null;

            // For Retrys
            for (int retry = 0; retry < GetRetryCount(Config); retry++)
            {
                HttpClient client = GetHttpClient(Config);

                // If certificate validation is disabled, inject a callback to handle properly
                SecurityProtocolType prevProtocols = (SecurityProtocolType)0;
                HttpClientHelper.ConfigureCertificateValidation(
                    Config.ValidateCertificates,
                    out prevProtocols,
                    out RemoteCertificateValidationCallback prevValidator);

                serviceUrl = GetServiceUrl(candidateServiceUrls, ref indx);
                var requestUri = GetRequestUri(serviceUrl + "apps/" + appName + "/" + id + "/status", queryArgs);
                var request = GetRequestMessage(HttpMethod.Delete, requestUri);

                try
                {
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        _logger?.LogDebug("DeleteStatusOverrideAsync {RequestUri}, status: {StatusCode}, retry: {retry}", requestUri.ToMaskedString(), response.StatusCode, retry);
                        int statusCode = (int)response.StatusCode;
                        if (statusCode >= 200 && statusCode < 300)
                        {
                            Interlocked.Exchange(ref _serviceUrl, serviceUrl);
                            EurekaHttpResponse resp = new EurekaHttpResponse(response.StatusCode)
                            {
                                Headers = response.Headers
                            };
                            return resp;
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "DeleteStatusOverrideAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
                }
                finally
                {
                    DisposeHttpClient(client);
                    HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
                }

                Interlocked.CompareExchange(ref _serviceUrl, null, serviceUrl);
                AddToFailingServiceUrls(serviceUrl);
            }

            throw new EurekaTransportException("Retry limit reached; giving up on completing the DeleteStatusOverrideAsync request");
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

            IList<string> candidateServiceUrls = GetServiceUrlCandidates();
            int indx = 0;
            string serviceUrl = null;

            // For Retrys
            for (int retry = 0; retry < GetRetryCount(Config); retry++)
            {
                HttpClient client = GetHttpClient(Config);

                // If certificate validation is disabled, inject a callback to handle properly
                SecurityProtocolType prevProtocols = (SecurityProtocolType)0;
                HttpClientHelper.ConfigureCertificateValidation(
                    Config.ValidateCertificates,
                    out prevProtocols,
                    out RemoteCertificateValidationCallback prevValidator);

                serviceUrl = GetServiceUrl(candidateServiceUrls, ref indx);
                var requestUri = GetRequestUri(serviceUrl + "apps/" + appName + "/" + id + "/status", queryArgs);
                var request = GetRequestMessage(HttpMethod.Put, requestUri);

                try
                {
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        _logger?.LogDebug("StatusUpdateAsync {RequestUri}, status: {StatusCode}, retry: {retry}", requestUri.ToMaskedString(), response.StatusCode, retry);
                        int statusCode = (int)response.StatusCode;
                        if (statusCode >= 200 && statusCode < 300)
                        {
                            Interlocked.Exchange(ref _serviceUrl, serviceUrl);
                            EurekaHttpResponse resp = new EurekaHttpResponse(response.StatusCode)
                            {
                                Headers = response.Headers
                            };
                            return resp;
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "StatusUpdateAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
                }
                finally
                {
                    DisposeHttpClient(client);
                    HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
                }

                Interlocked.CompareExchange(ref _serviceUrl, null, serviceUrl);
                AddToFailingServiceUrls(serviceUrl);
            }

            throw new EurekaTransportException("Retry limit reached; giving up on completing the StatusUpdateAsync request");
        }

        public virtual void Shutdown()
        {
        }

        internal string FetchAccessToken()
        {
            var config = Config as EurekaClientOptions;
            if (config == null || string.IsNullOrEmpty(config.AccessTokenUri))
            {
                return null;
            }

            return HttpClientHelper.GetAccessToken(
                config.AccessTokenUri,
                config.ClientId,
                config.ClientSecret,
                DEFAULT_GETACCESSTOKEN_TIMEOUT,
                config.ValidateCertificates).Result;
        }

        internal IList<string> GetServiceUrlCandidates()
        {
            // Get latest set of Eureka server urls
            IList<string> candidateServiceUrls = MakeServiceUrls(Config.EurekaServerServiceUrls);

            lock (_lock)
            {
                // Keep any exsisting failing service urls still in the candidate list
                _failingServiceUrls = _failingServiceUrls.Intersect(candidateServiceUrls).ToList();

                // If enough hosts are bad, we have no choice but start over again
                int threshold = (int)Math.Round(candidateServiceUrls.Count * 0.67);

                if (_failingServiceUrls.Count == 0)
                {
                    // no-op
                }
                else if (_failingServiceUrls.Count >= threshold)
                {
                    _logger?.LogDebug("Clearing quarantined list of size {Count}", _failingServiceUrls.Count);
                    _failingServiceUrls.Clear();
                }
                else
                {
                    List<string> remainingHosts = new List<string>(candidateServiceUrls.Count);
                    foreach (string endpoint in candidateServiceUrls)
                    {
                        if (!_failingServiceUrls.Contains(endpoint))
                        {
                            remainingHosts.Add(endpoint);
                        }
                    }

                    candidateServiceUrls = remainingHosts;
                }
            }

            return candidateServiceUrls;
        }

        internal void AddToFailingServiceUrls(string serviceUrl)
        {
            if (string.IsNullOrEmpty(serviceUrl))
            {
                return;
            }

            lock (_lock)
            {
                if (!_failingServiceUrls.Contains(serviceUrl))
                {
                    _failingServiceUrls.Add(serviceUrl);
                }
            }
        }

        internal string GetServiceUrl(IList<string> candidateServiceUrls, ref int indx)
        {
            var serviceUrl = _serviceUrl;
            if (string.IsNullOrEmpty(serviceUrl))
            {
                if (indx >= candidateServiceUrls.Count)
                {
                    throw new EurekaTransportException("Cannot execute request on any known server");
                }

                serviceUrl = candidateServiceUrls[indx++];
            }

            return serviceUrl;
        }

        protected internal static IList<string> MakeServiceUrls(string serviceUrls)
        {
            List<string> results = new List<string>();
            var split = serviceUrls.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var serviceUrl in split)
            {
                results.Add(MakeServiceUrl(serviceUrl));
            }

            return results;
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

        protected internal HttpRequestMessage GetRequestMessage(HttpMethod method, Uri requestUri)
        {
            string rawUri = requestUri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
            string rawUserInfo = requestUri.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped);
            var request = new HttpRequestMessage(method, rawUri);

            if (!string.IsNullOrEmpty(rawUserInfo) && rawUserInfo.IndexOfAny(COLON_DELIMIT) > 0)
            {
                string[] userInfo = GetUserInfo(rawUserInfo);
                if (userInfo.Length >= 2)
                {
                    request = HttpClientHelper.GetRequestMessage(method, rawUri, userInfo[0], userInfo[1]);
                }
            }
            else
            {
                request = HttpClientHelper.GetRequestMessage(method, rawUri, FetchAccessToken);
            }

            foreach (var header in _headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            request.Headers.Add("Accept", "application/json");
            request.Headers.Add(HTTP_X_DISCOVERY_ALLOW_REDIRECT, "false");
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
            _headers = headers ?? throw new ArgumentNullException(nameof(headers));

            // Validate serviceUrls
            MakeServiceUrls(Config.EurekaServerServiceUrls);
        }

        protected virtual async Task<EurekaHttpResponse<InstanceInfo>> DoGetInstanceAsync(string path)
        {
            IList<string> candidateServiceUrls = GetServiceUrlCandidates();
            int indx = 0;
            string serviceUrl = null;

            // For Retrys
            for (int retry = 0; retry < GetRetryCount(Config); retry++)
            {
                HttpClient client = GetHttpClient(Config);

                // If certificate validation is disabled, inject a callback to handle properly
                SecurityProtocolType prevProtocols = (SecurityProtocolType)0;
                HttpClientHelper.ConfigureCertificateValidation(
                    Config.ValidateCertificates,
                    out prevProtocols,
                    out RemoteCertificateValidationCallback prevValidator);

                serviceUrl = GetServiceUrl(candidateServiceUrls, ref indx);
                var requestUri = GetRequestUri(serviceUrl + path);
                var request = GetRequestMessage(HttpMethod.Get, requestUri);

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
                            "DoGetInstanceAsync {RequestUri}, status: {StatusCode}, instanceInfo: {Instance}, retry: {retry}",
                           requestUri.ToMaskedString(),
                           response.StatusCode,
                           (infoResp != null) ? infoResp.ToString() : "null",
                           retry);
                        int statusCode = (int)response.StatusCode;
                        if ((statusCode >= 200 && statusCode < 300) || statusCode == 404)
                        {
                            Interlocked.Exchange(ref _serviceUrl, serviceUrl);
                            EurekaHttpResponse<InstanceInfo> resp = new EurekaHttpResponse<InstanceInfo>(response.StatusCode, infoResp)
                            {
                                Headers = response.Headers
                            };
                            return resp;
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "DoGetInstanceAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
                }
                finally
                {
                    DisposeHttpClient(client);
                    HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
                }

                Interlocked.CompareExchange(ref _serviceUrl, null, serviceUrl);
                AddToFailingServiceUrls(serviceUrl);
            }

            throw new EurekaTransportException("Retry limit reached; giving up on completing the DoGetInstanceAsync request");
        }

        protected virtual async Task<EurekaHttpResponse<Applications>> DoGetApplicationsAsync(string path, ISet<string> regions)
        {
            string regionParams = CommaDelimit(regions);

            var queryArgs = new Dictionary<string, string>();
            if (regionParams != null)
            {
                queryArgs.Add("regions", regionParams);
            }

            IList<string> candidateServiceUrls = GetServiceUrlCandidates();
            int indx = 0;
            string serviceUrl = null;

            // For Retrys
            for (int retry = 0; retry < GetRetryCount(Config); retry++)
            {
                HttpClient client = GetHttpClient(Config);

                // If certificate validation is disabled, inject a callback to handle properly
                SecurityProtocolType prevProtocols = (SecurityProtocolType)0;
                HttpClientHelper.ConfigureCertificateValidation(
                    Config.ValidateCertificates,
                    out prevProtocols,
                    out RemoteCertificateValidationCallback prevValidator);

                serviceUrl = GetServiceUrl(candidateServiceUrls, ref indx);
                var requestUri = GetRequestUri(serviceUrl + path, queryArgs);
                var request = GetRequestMessage(HttpMethod.Get, requestUri);

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
                            "DoGetApplicationsAsync {RequestUri}, status: {StatusCode}, applications: {Application}, retry: {retry}",
                            requestUri.ToMaskedString(),
                            response.StatusCode,
                            (appsResp != null) ? appsResp.ToString() : "null",
                            retry);
                        int statusCode = (int)response.StatusCode;
                        if ((statusCode >= 200 && statusCode < 300) || statusCode == 404)
                        {
                            Interlocked.Exchange(ref _serviceUrl, serviceUrl);
                            EurekaHttpResponse<Applications> resp = new EurekaHttpResponse<Applications>(response.StatusCode, appsResp)
                            {
                                Headers = response.Headers
                            };
                            return resp;
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "DoGetApplicationsAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
                }
                finally
                {
                    DisposeHttpClient(client);
                    HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
                }

                Interlocked.CompareExchange(ref _serviceUrl, null, serviceUrl);
                AddToFailingServiceUrls(serviceUrl);
            }

            throw new EurekaTransportException("Retry limit reached; giving up on completing the DoGetApplicationsAsync request");
        }

        protected virtual HttpClient GetHttpClient(IEurekaClientConfig config)
        {
            if (_client != null)
            {
                return _client;
            }

            if (_handlerProvider != null)
            {
                return HttpClientHelper.GetHttpClient(config.ValidateCertificates, _handlerProvider.GetHttpClientHandler(), config.EurekaServerConnectTimeoutSeconds * 1000);
            }

            if (!string.IsNullOrEmpty(config.ProxyHost))
            {
                var proxyHandler = new HttpClientHandler();
                proxyHandler.Proxy = new WebProxy(config.ProxyHost, config.ProxyPort);
                if (!string.IsNullOrEmpty(config.ProxyPassword))
                {
                    proxyHandler.Proxy.Credentials = new NetworkCredential(config.ProxyUserName, config.ProxyPassword);
                }

                if (config.ShouldGZipContent)
                {
                    proxyHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                }

                return HttpClientHelper.GetHttpClient(config.ValidateCertificates, proxyHandler, config.EurekaServerConnectTimeoutSeconds * 1000);
            }

            if (config.ShouldGZipContent)
            {
                var gzipHandler = new HttpClientHandler();
                gzipHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                return HttpClientHelper.GetHttpClient(config.ValidateCertificates, gzipHandler, config.EurekaServerConnectTimeoutSeconds * 1000);
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

        private int GetRetryCount(IEurekaClientConfig config)
        {
            EurekaClientConfig clientConfig = config as EurekaClientConfig;
            return clientConfig == null ? DEFAULT_NUMBER_OF_RETRIES : clientConfig.EurekaServerRetryCount;
        }
    }
}
