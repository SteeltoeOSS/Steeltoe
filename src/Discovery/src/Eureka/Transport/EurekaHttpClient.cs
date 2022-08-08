// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;
using System.Net.Security;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.Http;
using Steeltoe.Common.Util;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Util;

namespace Steeltoe.Discovery.Eureka.Transport;

public class EurekaHttpClient : IEurekaHttpClient
{
    private const int DefaultNumberOfRetries = 3;
    private const string HttpXDiscoveryAllowRedirect = "X-Discovery-AllowRedirect";
    private const int DefaultGetAccessTokenTimeout = 10000; // Milliseconds

    private static readonly char[] ColonDelimit =
    {
        ':'
    };

    private readonly IOptionsMonitor<EurekaClientOptions> _configOptions;

    protected object @lock = new();
    protected IList<string> failingServiceUrls = new List<string>();

    protected IDictionary<string, string> headers;

    protected IEurekaClientConfig config;
    protected IHttpClientHandlerProvider handlerProvider;

    protected HttpClient httpClient;
    protected ILogger logger;
    protected internal string ServiceUrl;

    private JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected virtual IEurekaClientConfig Config => _configOptions != null ? _configOptions.CurrentValue : config;

    public EurekaHttpClient(IOptionsMonitor<EurekaClientOptions> config, IHttpClientHandlerProvider handlerProvider = null, ILoggerFactory logFactory = null)
    {
        this.config = null;
        _configOptions = config ?? throw new ArgumentNullException(nameof(config));
        this.handlerProvider = handlerProvider;
        Initialize(new Dictionary<string, string>(), logFactory);
    }

    public EurekaHttpClient(IEurekaClientConfig config, HttpClient client, ILoggerFactory logFactory = null)
        : this(config, new Dictionary<string, string>(), logFactory)
    {
        httpClient = client;
    }

    public EurekaHttpClient(IEurekaClientConfig config, ILoggerFactory logFactory = null, IHttpClientHandlerProvider handlerProvider = null)
        : this(config, new Dictionary<string, string>(), logFactory, handlerProvider)
    {
    }

    public EurekaHttpClient(IEurekaClientConfig config, IDictionary<string, string> headers, ILoggerFactory logFactory = null,
        IHttpClientHandlerProvider handlerProvider = null)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.handlerProvider = handlerProvider;
        Initialize(headers, logFactory);
    }

    protected EurekaHttpClient()
    {
    }

    public virtual Task<EurekaHttpResponse> RegisterAsync(InstanceInfo info)
    {
        ArgumentGuard.NotNull(info);

        return RegisterInternalAsync(info);
    }

    private async Task<EurekaHttpResponse> RegisterInternalAsync(InstanceInfo info)
    {
        if ((Platform.IsContainerized || Platform.IsCloudHosted) && info.HostName?.Equals("localhost", StringComparison.InvariantCultureIgnoreCase) == true)
        {
            logger?.LogWarning(
                "Registering with hostname 'localhost' in containerized or cloud environments may not be valid. Please configure Eureka:Instance:HostName with a non-localhost address");
        }

        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        string serviceUrl = null;
        httpClient ??= GetHttpClient(Config);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Config); retry++)
        {
            // If certificate validation is disabled, inject a callback to handle properly
            HttpClientHelper.ConfigureCertificateValidation(Config.ValidateCertificates, out SecurityProtocolType prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);

            serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{info.AppName}");
            HttpRequestMessage request = GetRequestMessage(HttpMethod.Post, requestUri);

            try
            {
                request.Content = GetRequestContent(new JsonInstanceInfoRoot
                {
                    Instance = info.ToJsonInstance()
                });

                using HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);
                logger?.LogDebug("RegisterAsync {RequestUri}, status: {StatusCode}, retry: {retry}", requestUri.ToMaskedString(), response.StatusCode, retry);
                int statusCode = (int)response.StatusCode;

                if ((statusCode >= 200 && statusCode < 300) || statusCode == 404)
                {
                    Interlocked.Exchange(ref ServiceUrl, serviceUrl);

                    var resp = new EurekaHttpResponse(response.StatusCode)
                    {
                        Headers = response.Headers
                    };

                    return resp;
                }

                string jsonError = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                logger?.LogInformation("Failure during RegisterAsync: {jsonError}", jsonError);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "RegisterAsync Failed, request was made to {requestUri}, retry: {retry}", requestUri.ToMaskedUri(), retry);
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the RegisterAsync request");
    }

    public virtual Task<EurekaHttpResponse<InstanceInfo>> SendHeartBeatAsync(string appName, string id, InstanceInfo info, InstanceStatus overriddenStatus)
    {
        ArgumentGuard.NotNull(info);

        if (string.IsNullOrEmpty(appName))
        {
            throw new ArgumentException(nameof(appName));
        }

        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentException(nameof(id));
        }

        return SendHeartBeatInternalAsync(appName, id, info, overriddenStatus);
    }

    private async Task<EurekaHttpResponse<InstanceInfo>> SendHeartBeatInternalAsync(string appName, string id, InstanceInfo info,
        InstanceStatus overriddenStatus)
    {
        var queryArgs = new Dictionary<string, string>
        {
            { "status", info.Status.ToSnakeCaseString(SnakeCaseStyle.AllCaps) },
            { "lastDirtyTimestamp", DateTimeConversions.ToJavaMillis(new DateTime(info.LastDirtyTimestamp, DateTimeKind.Utc)).ToString() }
        };

        if (overriddenStatus != InstanceStatus.Unknown)
        {
            queryArgs.Add("overriddenstatus", overriddenStatus.ToString());
        }

        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        string serviceUrl = null;
        httpClient ??= GetHttpClient(Config);

        for (int retry = 0; retry < GetRetryCount(Config); retry++)
        {
            // If certificate validation is disabled, inject a callback to handle properly
            HttpClientHelper.ConfigureCertificateValidation(Config.ValidateCertificates, out SecurityProtocolType prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);

            serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{info.AppName}/{id}", queryArgs);
            HttpRequestMessage request = GetRequestMessage(HttpMethod.Put, requestUri);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);
                JsonInstanceInfo instanceInfo = null;

                try
                {
                    instanceInfo = await response.Content.ReadFromJsonAsync<JsonInstanceInfo>(JsonSerializerOptions);
                }
                catch (Exception e)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode && string.IsNullOrEmpty(responseBody))
                    {
                        // request was successful but body was empty. This is OK, we don't need a response body
                    }
                    else
                    {
                        logger?.LogError(e, "Failed to read heartbeat response. Response code: {responseCode}, Body: {responseBody}", response.StatusCode,
                            responseBody);
                    }
                }

                InstanceInfo infoResp = null;

                if (instanceInfo != null)
                {
                    infoResp = InstanceInfo.FromJsonInstance(instanceInfo);
                }

                logger?.LogDebug("SendHeartbeatAsync {RequestUri}, status: {StatusCode}, instanceInfo: {Instance}, retry: {retry}", requestUri.ToMaskedString(),
                    response.StatusCode, infoResp != null ? infoResp.ToString() : "null", retry);

                int statusCode = (int)response.StatusCode;

                if ((statusCode >= 200 && statusCode < 300) || statusCode == 404)
                {
                    Interlocked.Exchange(ref ServiceUrl, serviceUrl);

                    var resp = new EurekaHttpResponse<InstanceInfo>(response.StatusCode, infoResp)
                    {
                        Headers = response.Headers
                    };

                    return resp;
                }
            }
            catch (Exception e)
            {
                logger?.LogError(e, "SendHeartBeatAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the SendHeartBeatAsync request");
    }

    public virtual Task<EurekaHttpResponse<Applications>> GetApplicationsAsync(ISet<string> regions = null)
    {
        return DoGetApplicationsAsync("apps/", regions);
    }

    public virtual Task<EurekaHttpResponse<Applications>> GetDeltaAsync(ISet<string> regions = null)
    {
        return DoGetApplicationsAsync("apps/delta", regions);
    }

    public virtual Task<EurekaHttpResponse<Applications>> GetVipAsync(string vipAddress, ISet<string> regions = null)
    {
        if (string.IsNullOrEmpty(vipAddress))
        {
            throw new ArgumentException(nameof(vipAddress));
        }

        return GetVipInternalAsync(vipAddress, regions);
    }

    private Task<EurekaHttpResponse<Applications>> GetVipInternalAsync(string vipAddress, ISet<string> regions)
    {
        return DoGetApplicationsAsync($"vips/{vipAddress}", regions);
    }

    public virtual Task<EurekaHttpResponse<Applications>> GetSecureVipAsync(string secureVipAddress, ISet<string> regions = null)
    {
        if (string.IsNullOrEmpty(secureVipAddress))
        {
            throw new ArgumentException(nameof(secureVipAddress));
        }

        return GetSecureVipInternalAsync(secureVipAddress, regions);
    }

    private Task<EurekaHttpResponse<Applications>> GetSecureVipInternalAsync(string secureVipAddress, ISet<string> regions = null)
    {
        return DoGetApplicationsAsync($"vips/{secureVipAddress}", regions);
    }

    public virtual Task<EurekaHttpResponse<Application>> GetApplicationAsync(string appName)
    {
        if (string.IsNullOrEmpty(appName))
        {
            throw new ArgumentException(nameof(appName));
        }

        return GetApplicationInternalAsync(appName);
    }

    private async Task<EurekaHttpResponse<Application>> GetApplicationInternalAsync(string appName)
    {
        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        string serviceUrl = null;
        httpClient ??= GetHttpClient(Config);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Config); retry++)
        {
            HttpClientHelper.ConfigureCertificateValidation(Config.ValidateCertificates, out SecurityProtocolType prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);

            serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{appName}");
            HttpRequestMessage request = GetRequestMessage(HttpMethod.Get, requestUri);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);

                JsonApplicationRoot applicationRoot =
                    await response.Content.ReadFromJsonAsync<JsonApplicationRoot>(JsonSerializerOptions).ConfigureAwait(false);

                Application appResp = null;

                if (applicationRoot != null)
                {
                    appResp = Application.FromJsonApplication(applicationRoot.Application);
                }

                logger?.LogDebug("GetApplicationAsync {RequestUri}, status: {StatusCode}, application: {Application}, retry: {retry}",
                    requestUri.ToMaskedString(), response.StatusCode, appResp != null ? appResp.ToString() : "null", retry);

                int statusCode = (int)response.StatusCode;

                if ((statusCode >= 200 && statusCode < 300) || statusCode == 403 || statusCode == 404)
                {
                    Interlocked.Exchange(ref ServiceUrl, serviceUrl);

                    var resp = new EurekaHttpResponse<Application>(response.StatusCode, appResp)
                    {
                        Headers = response.Headers
                    };

                    return resp;
                }
            }
            catch (Exception e)
            {
                logger?.LogError(e, "GetApplicationAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the GetApplicationAsync request");
    }

    public virtual Task<EurekaHttpResponse<InstanceInfo>> GetInstanceAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentException(nameof(id));
        }

        return GetInstanceInternalAsync(id);
    }

    public virtual Task<EurekaHttpResponse<InstanceInfo>> GetInstanceAsync(string appName, string id)
    {
        if (string.IsNullOrEmpty(appName))
        {
            throw new ArgumentException(nameof(appName));
        }

        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentException(nameof(id));
        }

        return GetInstanceInternalAsync(appName, id);
    }

    private Task<EurekaHttpResponse<InstanceInfo>> GetInstanceInternalAsync(string id)
    {
        return DoGetInstanceAsync($"instances/{id}");
    }

    private Task<EurekaHttpResponse<InstanceInfo>> GetInstanceInternalAsync(string appName, string id)
    {
        return DoGetInstanceAsync($"apps/{appName}/{id}");
    }

    public virtual Task<EurekaHttpResponse> CancelAsync(string appName, string id)
    {
        if (string.IsNullOrEmpty(appName))
        {
            throw new ArgumentException(nameof(appName));
        }

        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentException(nameof(id));
        }

        return CancelInternalAsync(appName, id);
    }

    private async Task<EurekaHttpResponse> CancelInternalAsync(string appName, string id)
    {
        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        string serviceUrl = null;
        httpClient ??= GetHttpClient(Config);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Config); retry++)
        {
            // If certificate validation is disabled, inject a callback to handle properly
            HttpClientHelper.ConfigureCertificateValidation(Config.ValidateCertificates, out SecurityProtocolType prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);

            serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{appName}/{id}");
            HttpRequestMessage request = GetRequestMessage(HttpMethod.Delete, requestUri);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);
                logger?.LogDebug("CancelAsync {RequestUri}, status: {StatusCode}, retry: {retry}", requestUri.ToMaskedString(), response.StatusCode, retry);
                Interlocked.Exchange(ref ServiceUrl, serviceUrl);

                var resp = new EurekaHttpResponse(response.StatusCode)
                {
                    Headers = response.Headers
                };

                return resp;
            }
            catch (Exception e)
            {
                logger?.LogError(e, "CancelAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the CancelAsync request");
    }

    public virtual Task<EurekaHttpResponse> DeleteStatusOverrideAsync(string appName, string id, InstanceInfo info)
    {
        if (string.IsNullOrEmpty(appName))
        {
            throw new ArgumentException(nameof(appName));
        }

        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentException(nameof(id));
        }

        ArgumentGuard.NotNull(info);

        return DeleteStatusOverrideInternalAsync(appName, id, info);
    }

    private async Task<EurekaHttpResponse> DeleteStatusOverrideInternalAsync(string appName, string id, InstanceInfo info)
    {
        var queryArgs = new Dictionary<string, string>
        {
            { "lastDirtyTimestamp", DateTimeConversions.ToJavaMillis(new DateTime(info.LastDirtyTimestamp, DateTimeKind.Utc)).ToString() }
        };

        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        string serviceUrl = null;
        httpClient ??= GetHttpClient(Config);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Config); retry++)
        {
            // If certificate validation is disabled, inject a callback to handle properly
            HttpClientHelper.ConfigureCertificateValidation(Config.ValidateCertificates, out SecurityProtocolType prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);

            serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{appName}/{id}/status", queryArgs);
            HttpRequestMessage request = GetRequestMessage(HttpMethod.Delete, requestUri);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);

                logger?.LogDebug("DeleteStatusOverrideAsync {RequestUri}, status: {StatusCode}, retry: {retry}", requestUri.ToMaskedString(),
                    response.StatusCode, retry);

                int statusCode = (int)response.StatusCode;

                if (statusCode >= 200 && statusCode < 300)
                {
                    Interlocked.Exchange(ref ServiceUrl, serviceUrl);

                    var resp = new EurekaHttpResponse(response.StatusCode)
                    {
                        Headers = response.Headers
                    };

                    return resp;
                }
            }
            catch (Exception e)
            {
                logger?.LogError(e, "DeleteStatusOverrideAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the DeleteStatusOverrideAsync request");
    }

    public virtual Task<EurekaHttpResponse> StatusUpdateAsync(string appName, string id, InstanceStatus newStatus, InstanceInfo info)
    {
        if (string.IsNullOrEmpty(appName))
        {
            throw new ArgumentException(nameof(appName));
        }

        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentException(nameof(id));
        }

        ArgumentGuard.NotNull(info);

        return StatusUpdateInternalAsync(appName, id, newStatus, info);
    }

    private async Task<EurekaHttpResponse> StatusUpdateInternalAsync(string appName, string id, InstanceStatus newStatus, InstanceInfo info)
    {
        var queryArgs = new Dictionary<string, string>
        {
            { "value", newStatus.ToSnakeCaseString(SnakeCaseStyle.AllCaps) },
            { "lastDirtyTimestamp", DateTimeConversions.ToJavaMillis(new DateTime(info.LastDirtyTimestamp, DateTimeKind.Utc)).ToString() }
        };

        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        string serviceUrl = null;
        httpClient ??= GetHttpClient(Config);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Config); retry++)
        {
            // If certificate validation is disabled, inject a callback to handle properly
            HttpClientHelper.ConfigureCertificateValidation(Config.ValidateCertificates, out SecurityProtocolType prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);

            serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{appName}/{id}/status", queryArgs);
            HttpRequestMessage request = GetRequestMessage(HttpMethod.Put, requestUri);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);

                logger?.LogDebug("StatusUpdateAsync {RequestUri}, status: {StatusCode}, retry: {retry}", requestUri.ToMaskedString(), response.StatusCode,
                    retry);

                int statusCode = (int)response.StatusCode;

                if (statusCode >= 200 && statusCode < 300)
                {
                    Interlocked.Exchange(ref ServiceUrl, serviceUrl);

                    var resp = new EurekaHttpResponse(response.StatusCode)
                    {
                        Headers = response.Headers
                    };

                    return resp;
                }
            }
            catch (Exception e)
            {
                logger?.LogError(e, "StatusUpdateAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the StatusUpdateAsync request");
    }

    public virtual void Shutdown()
    {
    }

    internal string FetchAccessToken()
    {
        return Config is not EurekaClientOptions config || string.IsNullOrEmpty(config.AccessTokenUri)
            ? null
            : HttpClientHelper.GetAccessTokenAsync(
                    config.AccessTokenUri, config.ClientId, config.ClientSecret, DefaultGetAccessTokenTimeout, config.ValidateCertificates).GetAwaiter()
                .GetResult();
    }

    internal IList<string> GetServiceUrlCandidates()
    {
        // Get latest set of Eureka server urls
        IList<string> candidateServiceUrls = MakeServiceUrls(Config.EurekaServerServiceUrls);

        lock (@lock)
        {
            // Keep any existing failing service urls still in the candidate list
            failingServiceUrls = failingServiceUrls.Intersect(candidateServiceUrls).ToList();

            // If enough hosts are bad, we have no choice but start over again
            int threshold = (int)Math.Round(candidateServiceUrls.Count * 0.67);

            if (failingServiceUrls.Count == 0)
            {
                // no-op
            }
            else if (failingServiceUrls.Count >= threshold)
            {
                logger?.LogDebug("Clearing quarantined list of size {Count}", failingServiceUrls.Count);
                failingServiceUrls.Clear();
            }
            else
            {
                var remainingHosts = new List<string>(candidateServiceUrls.Count);

                foreach (string endpoint in candidateServiceUrls)
                {
                    if (!failingServiceUrls.Contains(endpoint))
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

        lock (@lock)
        {
            if (!failingServiceUrls.Contains(serviceUrl))
            {
                failingServiceUrls.Add(serviceUrl);
            }
        }
    }

    internal string GetServiceUrl(IList<string> candidateServiceUrls, ref int index)
    {
        string serviceUrl = ServiceUrl;

        if (string.IsNullOrEmpty(serviceUrl))
        {
            if (index >= candidateServiceUrls.Count)
            {
                throw new EurekaTransportException("Cannot execute request on any known server");
            }

            serviceUrl = candidateServiceUrls[index++];
        }

        return serviceUrl;
    }

    protected internal static IList<string> MakeServiceUrls(string serviceUrls)
    {
        var results = new List<string>();

        string[] split = serviceUrls.Split(new[]
        {
            ','
        }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string serviceUrl in split)
        {
            results.Add(MakeServiceUrl(serviceUrl));
        }

        return results;
    }

    protected internal static string MakeServiceUrl(string serviceUrl)
    {
        string url = new Uri(serviceUrl).ToString();

        if (!url.EndsWith("/"))
        {
            url += '/';
        }

        return url;
    }

    protected internal HttpRequestMessage GetRequestMessage(HttpMethod method, Uri requestUri)
    {
        string rawUri = requestUri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
        string rawUserInfo = requestUri.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped);
        var request = new HttpRequestMessage(method, rawUri);

        if (!string.IsNullOrEmpty(rawUserInfo) && rawUserInfo.IndexOfAny(ColonDelimit) >= 0)
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

        foreach (KeyValuePair<string, string> header in headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }

        request.Headers.Add("Accept", "application/json");
        request.Headers.Add(HttpXDiscoveryAllowRedirect, "false");
        return request;
    }

    protected internal virtual Uri GetRequestUri(string baseUri, IDictionary<string, string> queryValues = null)
    {
        string uri = baseUri;

        if (queryValues != null)
        {
            var sb = new StringBuilder();
            string sep = "?";

            foreach (KeyValuePair<string, string> kvp in queryValues)
            {
                sb.Append($"{sep}{kvp.Key}={kvp.Value}");
                sep = "&";
            }

            uri += sb;
        }

        return new Uri(uri);
    }

    protected void Initialize(IDictionary<string, string> headers, ILoggerFactory logFactory)
    {
        logger = logFactory?.CreateLogger<EurekaHttpClient>();
        this.headers = headers ?? throw new ArgumentNullException(nameof(headers));
        JsonSerializerOptions.Converters.Add(new JsonInstanceInfoConverter());

        // Validate serviceUrls
        MakeServiceUrls(Config.EurekaServerServiceUrls);
    }

    protected virtual async Task<EurekaHttpResponse<InstanceInfo>> DoGetInstanceAsync(string path)
    {
        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        string serviceUrl = null;
        httpClient ??= GetHttpClient(Config);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Config); retry++)
        {
            // If certificate validation is disabled, inject a callback to handle properly
            HttpClientHelper.ConfigureCertificateValidation(Config.ValidateCertificates, out SecurityProtocolType prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);

            serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri(serviceUrl + path);
            HttpRequestMessage request = GetRequestMessage(HttpMethod.Get, requestUri);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);
                JsonInstanceInfoRoot infoRoot = await response.Content.ReadFromJsonAsync<JsonInstanceInfoRoot>(JsonSerializerOptions).ConfigureAwait(false);

                InstanceInfo infoResp = null;

                if (infoRoot != null)
                {
                    infoResp = InstanceInfo.FromJsonInstance(infoRoot.Instance);
                }

                logger?.LogDebug("DoGetInstanceAsync {RequestUri}, status: {StatusCode}, instanceInfo: {Instance}, retry: {retry}", requestUri.ToMaskedString(),
                    response.StatusCode, infoResp != null ? infoResp.ToString() : "null", retry);

                int statusCode = (int)response.StatusCode;

                if ((statusCode >= 200 && statusCode < 300) || statusCode == 404)
                {
                    Interlocked.Exchange(ref ServiceUrl, serviceUrl);

                    var resp = new EurekaHttpResponse<InstanceInfo>(response.StatusCode, infoResp)
                    {
                        Headers = response.Headers
                    };

                    return resp;
                }
            }
            catch (Exception e)
            {
                logger?.LogError(e, "DoGetInstanceAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
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
        int index = 0;
        string serviceUrl = null;
        httpClient ??= GetHttpClient(Config);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Config); retry++)
        {
            // If certificate validation is disabled, inject a callback to handle properly
            HttpClientHelper.ConfigureCertificateValidation(Config.ValidateCertificates, out SecurityProtocolType prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);

            serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri(serviceUrl + path, queryArgs);
            HttpRequestMessage request = GetRequestMessage(HttpMethod.Get, requestUri);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);
                JsonApplicationsRoot root = null;

                try
                {
                    root = await response.Content.ReadFromJsonAsync<JsonApplicationsRoot>(JsonSerializerOptions).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    logger?.LogInformation(e, "Failed to deserialize response");
                }

                Applications appsResp = null;

                if (response.StatusCode == HttpStatusCode.OK && root != null)
                {
                    appsResp = Applications.FromJsonApplications(root.Applications);
                }

                logger?.LogDebug("DoGetApplicationsAsync {RequestUri}, status: {StatusCode}, applications: {Application}, retry: {retry}",
                    requestUri.ToMaskedString(), response.StatusCode, appsResp != null ? appsResp.ToString() : "null", retry);

                int statusCode = (int)response.StatusCode;

                if ((statusCode >= 200 && statusCode < 300) || statusCode == 404)
                {
                    Interlocked.Exchange(ref ServiceUrl, serviceUrl);

                    var resp = new EurekaHttpResponse<Applications>(response.StatusCode, appsResp)
                    {
                        Headers = response.Headers
                    };

                    return resp;
                }
            }
            catch (Exception e)
            {
                logger?.LogError(e, "DoGetApplicationsAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(Config.ValidateCertificates, prevProtocols, prevValidator);
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the DoGetApplicationsAsync request");
    }

    protected virtual HttpClient GetHttpClient(IEurekaClientConfig config)
    {
        return httpClient ?? HttpClientHelper.GetHttpClient(config.ValidateCertificates,
            ConfigureEurekaHttpClientHandler(config, handlerProvider?.GetHttpClientHandler()), config.EurekaServerConnectTimeoutSeconds * 1000);
    }

    internal static HttpClientHandler ConfigureEurekaHttpClientHandler(IEurekaClientConfig config, HttpClientHandler handler)
    {
        handler ??= new HttpClientHandler();

        if (!string.IsNullOrEmpty(config.ProxyHost))
        {
            handler.Proxy = new WebProxy(config.ProxyHost, config.ProxyPort);

            if (!string.IsNullOrEmpty(config.ProxyPassword))
            {
                handler.Proxy.Credentials = new NetworkCredential(config.ProxyUserName, config.ProxyPassword);
            }
        }

        if (config.ShouldGZipContent)
        {
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        }

        return handler;
    }

    protected virtual HttpContent GetRequestContent(object toSerialize)
    {
        try
        {
            string json = JsonSerializer.Serialize(toSerialize);
            logger?.LogDebug($"GetRequestContent generated JSON: {json}");
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
        catch (Exception e)
        {
            logger?.LogError(e, "GetRequestContent Failed");
        }

        return new StringContent(string.Empty, Encoding.UTF8, "application/json");
    }

    private static string CommaDelimit(ICollection<string> toJoin)
    {
        if (toJoin == null || toJoin.Count == 0)
        {
            return null;
        }

        var sb = new StringBuilder();
        string sep = string.Empty;

        foreach (string value in toJoin)
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
            result = userInfo.Split(ColonDelimit);
        }

        return result;
    }

    private int GetRetryCount(IEurekaClientConfig config)
    {
        return config is EurekaClientConfig clientConfig ? clientConfig.EurekaServerRetryCount : DefaultNumberOfRetries;
    }
}
