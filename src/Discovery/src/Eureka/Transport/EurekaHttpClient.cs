// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
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

    private readonly IOptionsMonitor<EurekaClientOptions> _configurationOptions;

    protected object @lock = new();
    protected IList<string> failingServiceUrls = new List<string>();

    protected IDictionary<string, string> headers;

    protected IEurekaClientConfiguration configuration;
    protected IHttpClientHandlerProvider handlerProvider;

    protected HttpClient httpClient;
    protected ILogger logger;
    protected internal string ServiceUrl;

    private JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected virtual IEurekaClientConfiguration Configuration => _configurationOptions != null ? _configurationOptions.CurrentValue : configuration;

    public EurekaHttpClient(IOptionsMonitor<EurekaClientOptions> configuration, IHttpClientHandlerProvider handlerProvider = null,
        ILoggerFactory logFactory = null)
    {
        ArgumentGuard.NotNull(configuration);

        this.configuration = null;
        _configurationOptions = configuration;
        this.handlerProvider = handlerProvider;
        Initialize(new Dictionary<string, string>(), logFactory);
    }

    public EurekaHttpClient(IEurekaClientConfiguration configuration, HttpClient client, ILoggerFactory logFactory = null)
        : this(configuration, new Dictionary<string, string>(), logFactory)
    {
        httpClient = client;
    }

    public EurekaHttpClient(IEurekaClientConfiguration configuration, ILoggerFactory logFactory = null, IHttpClientHandlerProvider handlerProvider = null)
        : this(configuration, new Dictionary<string, string>(), logFactory, handlerProvider)
    {
    }

    public EurekaHttpClient(IEurekaClientConfiguration configuration, IDictionary<string, string> headers, ILoggerFactory logFactory = null,
        IHttpClientHandlerProvider handlerProvider = null)
    {
        ArgumentGuard.NotNull(configuration);

        this.configuration = configuration;
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
        if ((Platform.IsContainerized || Platform.IsCloudHosted) && info.HostName?.Equals("localhost", StringComparison.OrdinalIgnoreCase) == true)
        {
            logger?.LogWarning(
                "Registering with hostname 'localhost' in containerized or cloud environments may not be valid. Please configure Eureka:Instance:HostName with a non-localhost address");
        }

        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        string serviceUrl = null;
        httpClient ??= GetHttpClient(Configuration);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Configuration); retry++)
        {
            serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{info.AppName}");
            HttpRequestMessage request = GetRequestMessage(HttpMethod.Post, requestUri);

            try
            {
                request.Content = GetRequestContent(new JsonInstanceInfoRoot
                {
                    Instance = info.ToJsonInstance()
                });

                using HttpResponseMessage response = await httpClient.SendAsync(request);
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

                string jsonError = await response.Content.ReadAsStringAsync();
                logger?.LogInformation("Failure during RegisterAsync: {jsonError}", jsonError);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "RegisterAsync Failed, request was made to {requestUri}, retry: {retry}", requestUri.ToMaskedUri(), retry);
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the RegisterAsync request");
    }

    public virtual Task<EurekaHttpResponse<InstanceInfo>> SendHeartBeatAsync(string appName, string id, InstanceInfo info, InstanceStatus overriddenStatus)
    {
        ArgumentGuard.NotNull(info);

        ArgumentGuard.NotNullOrEmpty(appName);
        ArgumentGuard.NotNullOrEmpty(id);

        return SendHeartBeatInternalAsync(id, info, overriddenStatus);
    }

    private async Task<EurekaHttpResponse<InstanceInfo>> SendHeartBeatInternalAsync(string id, InstanceInfo info, InstanceStatus overriddenStatus)
    {
        var queryArgs = new Dictionary<string, string>
        {
            { "status", info.Status.ToSnakeCaseString(SnakeCaseStyle.AllCaps) },
            {
                "lastDirtyTimestamp",
                DateTimeConversions.ToJavaMillis(new DateTime(info.LastDirtyTimestamp, DateTimeKind.Utc)).ToString(CultureInfo.InvariantCulture)
            }
        };

        if (overriddenStatus != InstanceStatus.Unknown)
        {
            queryArgs.Add("overriddenstatus", overriddenStatus.ToString());
        }

        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        string serviceUrl = null;
        httpClient ??= GetHttpClient(Configuration);

        for (int retry = 0; retry < GetRetryCount(Configuration); retry++)
        {
            serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{info.AppName}/{id}", queryArgs);
            HttpRequestMessage request = GetRequestMessage(HttpMethod.Put, requestUri);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request);
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
        ArgumentGuard.NotNullOrEmpty(vipAddress);

        return GetVipInternalAsync(vipAddress, regions);
    }

    private Task<EurekaHttpResponse<Applications>> GetVipInternalAsync(string vipAddress, ISet<string> regions)
    {
        return DoGetApplicationsAsync($"vips/{vipAddress}", regions);
    }

    public virtual Task<EurekaHttpResponse<Applications>> GetSecureVipAsync(string secureVipAddress, ISet<string> regions = null)
    {
        ArgumentGuard.NotNullOrEmpty(secureVipAddress);

        return GetSecureVipInternalAsync(secureVipAddress, regions);
    }

    private Task<EurekaHttpResponse<Applications>> GetSecureVipInternalAsync(string secureVipAddress, ISet<string> regions = null)
    {
        return DoGetApplicationsAsync($"vips/{secureVipAddress}", regions);
    }

    public virtual Task<EurekaHttpResponse<Application>> GetApplicationAsync(string appName)
    {
        ArgumentGuard.NotNullOrEmpty(appName);

        return GetApplicationInternalAsync(appName);
    }

    private async Task<EurekaHttpResponse<Application>> GetApplicationInternalAsync(string appName)
    {
        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        string serviceUrl = null;
        httpClient ??= GetHttpClient(Configuration);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Configuration); retry++)
        {
            serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{appName}");
            HttpRequestMessage request = GetRequestMessage(HttpMethod.Get, requestUri);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request);

                var applicationRoot = await response.Content.ReadFromJsonAsync<JsonApplicationRoot>(JsonSerializerOptions);

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

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the GetApplicationAsync request");
    }

    public virtual Task<EurekaHttpResponse<InstanceInfo>> GetInstanceAsync(string id)
    {
        ArgumentGuard.NotNullOrEmpty(id);

        return GetInstanceInternalAsync(id);
    }

    public virtual Task<EurekaHttpResponse<InstanceInfo>> GetInstanceAsync(string appName, string id)
    {
        ArgumentGuard.NotNullOrEmpty(appName);
        ArgumentGuard.NotNullOrEmpty(id);

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
        ArgumentGuard.NotNullOrEmpty(appName);
        ArgumentGuard.NotNullOrEmpty(id);

        return CancelInternalAsync(appName, id);
    }

    private async Task<EurekaHttpResponse> CancelInternalAsync(string appName, string id)
    {
        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        string serviceUrl = null;
        httpClient ??= GetHttpClient(Configuration);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Configuration); retry++)
        {
            serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{appName}/{id}");
            HttpRequestMessage request = GetRequestMessage(HttpMethod.Delete, requestUri);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request);
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

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the CancelAsync request");
    }

    public virtual Task<EurekaHttpResponse> DeleteStatusOverrideAsync(string appName, string id, InstanceInfo info)
    {
        ArgumentGuard.NotNullOrEmpty(appName);
        ArgumentGuard.NotNullOrEmpty(id);

        ArgumentGuard.NotNull(info);

        return DeleteStatusOverrideInternalAsync(appName, id, info);
    }

    private async Task<EurekaHttpResponse> DeleteStatusOverrideInternalAsync(string appName, string id, InstanceInfo info)
    {
        var queryArgs = new Dictionary<string, string>
        {
            {
                "lastDirtyTimestamp",
                DateTimeConversions.ToJavaMillis(new DateTime(info.LastDirtyTimestamp, DateTimeKind.Utc)).ToString(CultureInfo.InvariantCulture)
            }
        };

        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        string serviceUrl = null;
        httpClient ??= GetHttpClient(Configuration);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Configuration); retry++)
        {
            serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{appName}/{id}/status", queryArgs);
            HttpRequestMessage request = GetRequestMessage(HttpMethod.Delete, requestUri);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request);

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

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the DeleteStatusOverrideAsync request");
    }

    public virtual Task<EurekaHttpResponse> StatusUpdateAsync(string appName, string id, InstanceStatus newStatus, InstanceInfo info)
    {
        ArgumentGuard.NotNullOrEmpty(appName);
        ArgumentGuard.NotNullOrEmpty(id);

        ArgumentGuard.NotNull(info);

        return StatusUpdateInternalAsync(appName, id, newStatus, info);
    }

    private async Task<EurekaHttpResponse> StatusUpdateInternalAsync(string appName, string id, InstanceStatus newStatus, InstanceInfo info)
    {
        var queryArgs = new Dictionary<string, string>
        {
            { "value", newStatus.ToSnakeCaseString(SnakeCaseStyle.AllCaps) },
            {
                "lastDirtyTimestamp",
                DateTimeConversions.ToJavaMillis(new DateTime(info.LastDirtyTimestamp, DateTimeKind.Utc)).ToString(CultureInfo.InvariantCulture)
            }
        };

        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        string serviceUrl = null;
        httpClient ??= GetHttpClient(Configuration);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Configuration); retry++)
        {
            serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{appName}/{id}/status", queryArgs);
            HttpRequestMessage request = GetRequestMessage(HttpMethod.Put, requestUri);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request);

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
        return Configuration is not EurekaClientOptions options || string.IsNullOrEmpty(options.AccessTokenUri)
            ? null
            : HttpClientHelper.GetAccessTokenAsync(
                    options.AccessTokenUri, options.ClientId, options.ClientSecret, DefaultGetAccessTokenTimeout, options.ValidateCertificates).GetAwaiter()
                .GetResult();
    }

    internal IList<string> GetServiceUrlCandidates()
    {
        // Get latest set of Eureka server urls
        IList<string> candidateServiceUrls = MakeServiceUrls(Configuration.EurekaServerServiceUrls);

        lock (@lock)
        {
            // Keep any existing failing service urls still in the candidate list
            failingServiceUrls = failingServiceUrls.Intersect(candidateServiceUrls).ToList();

            // If enough hosts are bad, we have no choice but start over again
            int threshold = (int)Math.Round(candidateServiceUrls.Count * 0.67);

            if (failingServiceUrls.Count == 0)
            {
                // Intentionally left empty.
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

        if (!url.EndsWith('/'))
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
        ArgumentGuard.NotNull(headers);

        logger = logFactory?.CreateLogger<EurekaHttpClient>();
        this.headers = headers;
        JsonSerializerOptions.Converters.Add(new JsonInstanceInfoConverter());

        // Validate serviceUrls
        MakeServiceUrls(Configuration.EurekaServerServiceUrls);
    }

    protected virtual async Task<EurekaHttpResponse<InstanceInfo>> DoGetInstanceAsync(string path)
    {
        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        string serviceUrl = null;
        httpClient ??= GetHttpClient(Configuration);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Configuration); retry++)
        {
            serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri(serviceUrl + path);
            HttpRequestMessage request = GetRequestMessage(HttpMethod.Get, requestUri);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request);
                var infoRoot = await response.Content.ReadFromJsonAsync<JsonInstanceInfoRoot>(JsonSerializerOptions);

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
        httpClient ??= GetHttpClient(Configuration);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Configuration); retry++)
        {
            serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri(serviceUrl + path, queryArgs);
            HttpRequestMessage request = GetRequestMessage(HttpMethod.Get, requestUri);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request);
                JsonApplicationsRoot root = null;

                try
                {
                    root = await response.Content.ReadFromJsonAsync<JsonApplicationsRoot>(JsonSerializerOptions);
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

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the DoGetApplicationsAsync request");
    }

    protected virtual HttpClient GetHttpClient(IEurekaClientConfiguration configuration)
    {
        return httpClient ?? HttpClientHelper.GetHttpClient(configuration.ValidateCertificates,
            ConfigureEurekaHttpClientHandler(configuration, handlerProvider?.GetHttpClientHandler()), configuration.EurekaServerConnectTimeoutSeconds * 1000);
    }

    internal static HttpClientHandler ConfigureEurekaHttpClientHandler(IEurekaClientConfiguration configuration, HttpClientHandler handler)
    {
        handler ??= new HttpClientHandler();

        if (!string.IsNullOrEmpty(configuration.ProxyHost))
        {
            handler.Proxy = new WebProxy(configuration.ProxyHost, configuration.ProxyPort);

            if (!string.IsNullOrEmpty(configuration.ProxyPassword))
            {
                handler.Proxy.Credentials = new NetworkCredential(configuration.ProxyUserName, configuration.ProxyPassword);
            }
        }

        if (configuration.ShouldGZipContent)
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

    private int GetRetryCount(IEurekaClientConfiguration configuration)
    {
        return configuration is EurekaClientConfiguration clientConfig ? clientConfig.EurekaServerRetryCount : DefaultNumberOfRetries;
    }
}
