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

public class EurekaHttpClient
{
    private const int DefaultNumberOfRetries = 3;
    private const string HttpXDiscoveryAllowRedirect = "X-Discovery-AllowRedirect";
    private const int DefaultGetAccessTokenTimeout = 10000; // Milliseconds

    private static readonly char[] ColonDelimit =
    {
        ':'
    };

    private readonly IOptionsMonitor<EurekaClientOptions> _configurationOptions;

    private readonly object _lock = new();
    protected IList<string> failingServiceUrls = new List<string>();

    protected IDictionary<string, string> headers;

    protected EurekaClientConfiguration configuration;
    protected IHttpClientHandlerProvider handlerProvider;

    protected HttpClient httpClient;
    protected ILogger logger;
    protected internal string ServiceUrl;

    private JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected virtual EurekaClientConfiguration Configuration => _configurationOptions != null ? _configurationOptions.CurrentValue : configuration;

    public EurekaHttpClient(IOptionsMonitor<EurekaClientOptions> configuration, IHttpClientHandlerProvider handlerProvider = null,
        ILoggerFactory logFactory = null)
    {
        ArgumentGuard.NotNull(configuration);

        this.configuration = null;
        _configurationOptions = configuration;
        this.handlerProvider = handlerProvider;
        Initialize(new Dictionary<string, string>(), logFactory);
    }

    public EurekaHttpClient(EurekaClientConfiguration configuration, HttpClient client, ILoggerFactory logFactory = null)
        : this(configuration, new Dictionary<string, string>(), logFactory)
    {
        httpClient = client;
    }

    public EurekaHttpClient(EurekaClientConfiguration configuration, ILoggerFactory logFactory = null, IHttpClientHandlerProvider handlerProvider = null)
        : this(configuration, new Dictionary<string, string>(), logFactory, handlerProvider)
    {
    }

    public EurekaHttpClient(EurekaClientConfiguration configuration, IDictionary<string, string> headers, ILoggerFactory logFactory = null,
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

    public virtual async Task<EurekaHttpResponse> RegisterAsync(InstanceInfo info, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(info);

        if ((Platform.IsContainerized || Platform.IsCloudHosted) && info.HostName?.Equals("localhost", StringComparison.OrdinalIgnoreCase) == true)
        {
            logger?.LogWarning(
                "Registering with hostname 'localhost' in containerized or cloud environments may not be valid. Please configure Eureka:Instance:HostName with a non-localhost address");
        }

        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        httpClient ??= GetHttpClient(Configuration);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Configuration); retry++)
        {
            string serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{info.AppName}");
            HttpRequestMessage request = await GetRequestMessageAsync(HttpMethod.Post, requestUri, cancellationToken);

            try
            {
                request.Content = GetRequestContent(new JsonInstanceInfoRoot
                {
                    Instance = info.ToJsonInstance()
                });

                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
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

                string jsonError = await response.Content.ReadAsStringAsync(cancellationToken);
                logger?.LogInformation("Failure during RegisterAsync: {jsonError}", jsonError);
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                logger?.LogError(exception, "RegisterAsync Failed, request was made to {requestUri}, retry: {retry}", requestUri.ToMaskedUri(), retry);
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the RegisterAsync request");
    }

    public virtual async Task<EurekaHttpResponse<InstanceInfo>> SendHeartBeatAsync(string appName, string id, InstanceInfo info,
        InstanceStatus overriddenStatus, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrEmpty(appName);
        ArgumentGuard.NotNullOrEmpty(id);
        ArgumentGuard.NotNull(info);

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
        httpClient ??= GetHttpClient(Configuration);

        for (int retry = 0; retry < GetRetryCount(Configuration); retry++)
        {
            string serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{info.AppName}/{id}", queryArgs);
            HttpRequestMessage request = await GetRequestMessageAsync(HttpMethod.Put, requestUri, cancellationToken);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
                JsonInstanceInfo instanceInfo = null;

                try
                {
                    instanceInfo = await response.Content.ReadFromJsonAsync<JsonInstanceInfo>(JsonSerializerOptions, cancellationToken);
                }
                catch (Exception exception) when (!exception.IsCancellation())
                {
                    string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                    if (response.IsSuccessStatusCode && string.IsNullOrEmpty(responseBody))
                    {
                        // request was successful but body was empty. This is OK, we don't need a response body
                    }

                    logger?.LogError(exception, "Failed to read heartbeat response. Response code: {responseCode}, Body: {responseBody}", response.StatusCode,
                        responseBody);
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
            catch (Exception exception) when (!exception.IsCancellation())
            {
                logger?.LogError(exception, "SendHeartBeatAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the SendHeartBeatAsync request");
    }

    public virtual Task<EurekaHttpResponse<Applications>> GetApplicationsAsync(CancellationToken cancellationToken)
    {
        return GetApplicationsAsync(null, cancellationToken);
    }

    public virtual Task<EurekaHttpResponse<Applications>> GetApplicationsAsync(ISet<string> regions, CancellationToken cancellationToken)
    {
        return DoGetApplicationsAsync("apps/", regions, cancellationToken);
    }

    public virtual Task<EurekaHttpResponse<Applications>> GetDeltaAsync(CancellationToken cancellationToken)
    {
        return GetDeltaAsync(null, cancellationToken);
    }

    public virtual Task<EurekaHttpResponse<Applications>> GetDeltaAsync(ISet<string> regions, CancellationToken cancellationToken)
    {
        return DoGetApplicationsAsync("apps/delta", regions, cancellationToken);
    }

    public virtual Task<EurekaHttpResponse<Applications>> GetVipAsync(string vipAddress, CancellationToken cancellationToken)
    {
        return GetVipAsync(vipAddress, null, cancellationToken);
    }

    public virtual Task<EurekaHttpResponse<Applications>> GetVipAsync(string vipAddress, ISet<string> regions, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrEmpty(vipAddress);

        return DoGetApplicationsAsync($"vips/{vipAddress}", regions, cancellationToken);
    }

    public virtual Task<EurekaHttpResponse<Applications>> GetSecureVipAsync(string secureVipAddress, CancellationToken cancellationToken)
    {
        return GetSecureVipAsync(secureVipAddress, null, cancellationToken);
    }

    public virtual Task<EurekaHttpResponse<Applications>> GetSecureVipAsync(string secureVipAddress, ISet<string> regions, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrEmpty(secureVipAddress);

        return DoGetApplicationsAsync($"vips/{secureVipAddress}", regions, cancellationToken);
    }

    public virtual async Task<EurekaHttpResponse<Application>> GetApplicationAsync(string appName, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrEmpty(appName);

        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        httpClient ??= GetHttpClient(Configuration);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Configuration); retry++)
        {
            string serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{appName}");
            HttpRequestMessage request = await GetRequestMessageAsync(HttpMethod.Get, requestUri, cancellationToken);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

                var applicationRoot = await response.Content.ReadFromJsonAsync<JsonApplicationRoot>(JsonSerializerOptions, cancellationToken);

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
            catch (Exception exception) when (!exception.IsCancellation())
            {
                logger?.LogError(exception, "GetApplicationAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the GetApplicationAsync request");
    }

    public virtual Task<EurekaHttpResponse<InstanceInfo>> GetInstanceAsync(string id, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrEmpty(id);

        return DoGetInstanceAsync($"instances/{id}", cancellationToken);
    }

    public virtual Task<EurekaHttpResponse<InstanceInfo>> GetInstanceAsync(string appName, string id, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrEmpty(appName);
        ArgumentGuard.NotNullOrEmpty(id);

        return DoGetInstanceAsync($"apps/{appName}/{id}", cancellationToken);
    }

    public virtual async Task<EurekaHttpResponse> CancelAsync(string appName, string id, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrEmpty(appName);
        ArgumentGuard.NotNullOrEmpty(id);

        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        httpClient ??= GetHttpClient(Configuration);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Configuration); retry++)
        {
            string serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{appName}/{id}");
            HttpRequestMessage request = await GetRequestMessageAsync(HttpMethod.Delete, requestUri, cancellationToken);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
                logger?.LogDebug("CancelAsync {RequestUri}, status: {StatusCode}, retry: {retry}", requestUri.ToMaskedString(), response.StatusCode, retry);
                Interlocked.Exchange(ref ServiceUrl, serviceUrl);

                var resp = new EurekaHttpResponse(response.StatusCode)
                {
                    Headers = response.Headers
                };

                return resp;
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                logger?.LogError(exception, "CancelAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the CancelAsync request");
    }

    public virtual async Task<EurekaHttpResponse> DeleteStatusOverrideAsync(string appName, string id, InstanceInfo info, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrEmpty(appName);
        ArgumentGuard.NotNullOrEmpty(id);
        ArgumentGuard.NotNull(info);

        var queryArgs = new Dictionary<string, string>
        {
            {
                "lastDirtyTimestamp",
                DateTimeConversions.ToJavaMillis(new DateTime(info.LastDirtyTimestamp, DateTimeKind.Utc)).ToString(CultureInfo.InvariantCulture)
            }
        };

        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        httpClient ??= GetHttpClient(Configuration);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Configuration); retry++)
        {
            string serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{appName}/{id}/status", queryArgs);
            HttpRequestMessage request = await GetRequestMessageAsync(HttpMethod.Delete, requestUri, cancellationToken);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

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
            catch (Exception exception) when (!exception.IsCancellation())
            {
                logger?.LogError(exception, "DeleteStatusOverrideAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the DeleteStatusOverrideAsync request");
    }

    public virtual async Task<EurekaHttpResponse> StatusUpdateAsync(string appName, string id, InstanceStatus newStatus, InstanceInfo info,
        CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrEmpty(appName);
        ArgumentGuard.NotNullOrEmpty(id);
        ArgumentGuard.NotNull(info);

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
        httpClient ??= GetHttpClient(Configuration);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Configuration); retry++)
        {
            string serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{appName}/{id}/status", queryArgs);
            HttpRequestMessage request = await GetRequestMessageAsync(HttpMethod.Put, requestUri, cancellationToken);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

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
            catch (Exception exception) when (!exception.IsCancellation())
            {
                logger?.LogError(exception, "StatusUpdateAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the StatusUpdateAsync request");
    }

    public virtual Task ShutdownAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task<string> FetchAccessTokenAsync(CancellationToken cancellationToken)
    {
        return Configuration is not EurekaClientOptions options || string.IsNullOrEmpty(options.AccessTokenUri)
            ? null
            : await HttpClientHelper.GetAccessTokenAsync(options.AccessTokenUri, options.ClientId, options.ClientSecret, DefaultGetAccessTokenTimeout,
                options.ValidateCertificates, null, null, cancellationToken);
    }

    internal IList<string> GetServiceUrlCandidates()
    {
        // Get latest set of Eureka server urls
        IList<string> candidateServiceUrls = MakeServiceUrls(Configuration.EurekaServerServiceUrls);

        lock (_lock)
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

        lock (_lock)
        {
            if (!failingServiceUrls.Contains(serviceUrl))
            {
                failingServiceUrls.Add(serviceUrl);
            }
        }
    }

    private string GetServiceUrl(IList<string> candidateServiceUrls, ref int index)
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

    private static IList<string> MakeServiceUrls(string serviceUrls)
    {
        var results = new List<string>();

        string[] split = serviceUrls.Split(new[]
        {
            ','
        }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

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

    protected internal async Task<HttpRequestMessage> GetRequestMessageAsync(HttpMethod method, Uri requestUri, CancellationToken cancellationToken)
    {
        var rawUri = new Uri(requestUri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped));
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
            request = await HttpClientHelper.GetRequestMessageAsync(method, rawUri, FetchAccessTokenAsync, cancellationToken);
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

    protected virtual async Task<EurekaHttpResponse<InstanceInfo>> DoGetInstanceAsync(string path, CancellationToken cancellationToken)
    {
        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        httpClient ??= GetHttpClient(Configuration);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Configuration); retry++)
        {
            string serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri(serviceUrl + path);
            HttpRequestMessage request = await GetRequestMessageAsync(HttpMethod.Get, requestUri, cancellationToken);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
                var infoRoot = await response.Content.ReadFromJsonAsync<JsonInstanceInfoRoot>(JsonSerializerOptions, cancellationToken);

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
            catch (Exception exception) when (!exception.IsCancellation())
            {
                logger?.LogError(exception, "DoGetInstanceAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the DoGetInstanceAsync request");
    }

    protected virtual async Task<EurekaHttpResponse<Applications>> DoGetApplicationsAsync(string path, ISet<string> regions,
        CancellationToken cancellationToken)
    {
        string regionParams = CommaDelimit(regions);

        var queryArgs = new Dictionary<string, string>();

        if (regionParams != null)
        {
            queryArgs.Add("regions", regionParams);
        }

        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        httpClient ??= GetHttpClient(Configuration);

        // For retries
        for (int retry = 0; retry < GetRetryCount(Configuration); retry++)
        {
            string serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri(serviceUrl + path, queryArgs);
            HttpRequestMessage request = await GetRequestMessageAsync(HttpMethod.Get, requestUri, cancellationToken);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
                JsonApplicationsRoot root = null;

                try
                {
                    root = await response.Content.ReadFromJsonAsync<JsonApplicationsRoot>(JsonSerializerOptions, cancellationToken);
                }
                catch (Exception exception) when (!exception.IsCancellation())
                {
                    logger?.LogInformation(exception, "Failed to deserialize response");
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
            catch (Exception exception) when (!exception.IsCancellation())
            {
                logger?.LogError(exception, "DoGetApplicationsAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the DoGetApplicationsAsync request");
    }

    protected virtual HttpClient GetHttpClient(EurekaClientConfiguration configuration)
    {
        return httpClient ?? HttpClientHelper.GetHttpClient(configuration.ValidateCertificates,
            ConfigureEurekaHttpClientHandler(configuration, handlerProvider?.GetHttpClientHandler()), configuration.EurekaServerConnectTimeoutSeconds * 1000);
    }

    internal static HttpClientHandler ConfigureEurekaHttpClientHandler(EurekaClientConfiguration configuration, HttpClientHandler handler)
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

    private int GetRetryCount(EurekaClientConfiguration configuration)
    {
        return configuration is EurekaClientConfiguration clientConfig ? clientConfig.EurekaServerRetryCount : DefaultNumberOfRetries;
    }
}
