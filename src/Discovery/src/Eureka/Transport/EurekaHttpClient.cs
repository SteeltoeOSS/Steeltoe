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
    private const string HttpXDiscoveryAllowRedirect = "X-Discovery-AllowRedirect";
    private const int DefaultGetAccessTokenTimeoutInMilliseconds = 10_000;
    private static readonly char[] ColonDelimiter = [':'];

    private readonly IOptionsMonitor<EurekaClientOptions> _optionsMonitor;
    private readonly object _lock = new();
    private readonly EurekaClientOptions _configuration;
    private IList<string> _failingServiceUrls = new List<string>();
    private ILogger _logger;
    internal string ServiceUrl;
    protected HttpClient httpClient;

    private JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private EurekaClientOptions Configuration => _optionsMonitor != null ? _optionsMonitor.CurrentValue : _configuration;

    // Constructor used by Dependency Injection
    public EurekaHttpClient(IOptionsMonitor<EurekaClientOptions> optionsMonitor, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(optionsMonitor);
        ArgumentGuard.NotNull(httpClientFactory);
        ArgumentGuard.NotNull(loggerFactory);

        _configuration = null;
        _optionsMonitor = optionsMonitor;
        httpClient = httpClientFactory.CreateClient("Eureka");
        Initialize(loggerFactory);
    }


    public EurekaHttpClient(IOptionsMonitor<EurekaClientOptions> optionsMonitor, ILoggerFactory loggerFactory = null)
    {
        ArgumentGuard.NotNull(optionsMonitor);

        _configuration = null;
        _optionsMonitor = optionsMonitor;
        Initialize(loggerFactory);
    }

    public async Task<EurekaHttpResponse> RegisterAsync(InstanceInfo info, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(info);

        if ((Platform.IsContainerized || Platform.IsCloudHosted) && info.HostName?.Equals("localhost", StringComparison.OrdinalIgnoreCase) == true)
        {
            _logger?.LogWarning(
                "Registering with hostname 'localhost' in containerized or cloud environments may not be valid. Please configure Eureka:Instance:HostName with a non-localhost address");
        }

        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        httpClient ??= GetHttpClient();

        // For retries
        for (int retry = 0; retry < GetRetryCount(); retry++)
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
                _logger?.LogDebug("RegisterAsync {RequestUri}, status: {StatusCode}, retry: {retry}", requestUri.ToMaskedString(), response.StatusCode, retry);
                int statusCode = (int)response.StatusCode;

                if ((statusCode >= 200 && statusCode < 300) || statusCode == 404)
                {
                    Interlocked.Exchange(ref ServiceUrl, serviceUrl);

                    return new EurekaHttpResponse(response.StatusCode, response.Headers);
                }

                string jsonError = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger?.LogInformation("Failure during RegisterAsync: {jsonError}", jsonError);
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger?.LogError(exception, "RegisterAsync Failed, request was made to {requestUri}, retry: {retry}", requestUri.ToMaskedUri(), retry);
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the RegisterAsync request");
    }

    public async Task<EurekaHttpResponse<InstanceInfo>> SendHeartBeatAsync(string appName, string id, InstanceInfo info,
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
        httpClient ??= GetHttpClient();

        for (int retry = 0; retry < GetRetryCount(); retry++)
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
                    else
                    {
                        _logger?.LogError(exception, "Failed to read heartbeat response. Response code: {responseCode}, Body: {responseBody}",
                            response.StatusCode, responseBody);
                    }
                }

                InstanceInfo infoResp = null;

                if (instanceInfo != null)
                {
                    infoResp = InstanceInfo.FromJsonInstance(instanceInfo);
                }

                _logger?.LogDebug("SendHeartbeatAsync {RequestUri}, status: {StatusCode}, instanceInfo: {Instance}, retry: {retry}",
                    requestUri.ToMaskedString(), response.StatusCode, infoResp != null ? infoResp.ToString() : "null", retry);

                int statusCode = (int)response.StatusCode;

                if ((statusCode >= 200 && statusCode < 300) || statusCode == 404)
                {
                    Interlocked.Exchange(ref ServiceUrl, serviceUrl);

                    return new EurekaHttpResponse<InstanceInfo>(response.StatusCode, response.Headers, infoResp);
                }
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger?.LogError(exception, "SendHeartBeatAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the SendHeartBeatAsync request");
    }

    public Task<EurekaHttpResponse<Applications>> GetApplicationsAsync(CancellationToken cancellationToken)
    {
        return GetApplicationsAsync(null, cancellationToken);
    }

    public Task<EurekaHttpResponse<Applications>> GetApplicationsAsync(ISet<string> regions, CancellationToken cancellationToken)
    {
        return DoGetApplicationsAsync("apps/", regions, cancellationToken);
    }

    public Task<EurekaHttpResponse<Applications>> GetDeltaAsync(CancellationToken cancellationToken)
    {
        return GetDeltaAsync(null, cancellationToken);
    }

    public Task<EurekaHttpResponse<Applications>> GetDeltaAsync(ISet<string> regions, CancellationToken cancellationToken)
    {
        return DoGetApplicationsAsync("apps/delta", regions, cancellationToken);
    }

    public Task<EurekaHttpResponse<Applications>> GetVipAsync(string vipAddress, CancellationToken cancellationToken)
    {
        return GetVipAsync(vipAddress, null, cancellationToken);
    }

    public Task<EurekaHttpResponse<Applications>> GetVipAsync(string vipAddress, ISet<string> regions, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrEmpty(vipAddress);

        return DoGetApplicationsAsync($"vips/{vipAddress}", regions, cancellationToken);
    }

    public Task<EurekaHttpResponse<Applications>> GetSecureVipAsync(string secureVipAddress, CancellationToken cancellationToken)
    {
        return GetSecureVipAsync(secureVipAddress, null, cancellationToken);
    }

    public Task<EurekaHttpResponse<Applications>> GetSecureVipAsync(string secureVipAddress, ISet<string> regions, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrEmpty(secureVipAddress);

        return DoGetApplicationsAsync($"vips/{secureVipAddress}", regions, cancellationToken);
    }

    public async Task<EurekaHttpResponse<Application>> GetApplicationAsync(string appName, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrEmpty(appName);

        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        httpClient ??= GetHttpClient();

        // For retries
        for (int retry = 0; retry < GetRetryCount(); retry++)
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

                _logger?.LogDebug("GetApplicationAsync {RequestUri}, status: {StatusCode}, application: {Application}, retry: {retry}",
                    requestUri.ToMaskedString(), response.StatusCode, appResp != null ? appResp.ToString() : "null", retry);

                int statusCode = (int)response.StatusCode;

                if ((statusCode >= 200 && statusCode < 300) || statusCode == 403 || statusCode == 404)
                {
                    Interlocked.Exchange(ref ServiceUrl, serviceUrl);

                    return new EurekaHttpResponse<Application>(response.StatusCode, response.Headers, appResp);
                }
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger?.LogError(exception, "GetApplicationAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the GetApplicationAsync request");
    }

    public Task<EurekaHttpResponse<InstanceInfo>> GetInstanceAsync(string id, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrEmpty(id);

        return DoGetInstanceAsync($"instances/{id}", cancellationToken);
    }

    public Task<EurekaHttpResponse<InstanceInfo>> GetInstanceAsync(string appName, string id, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrEmpty(appName);
        ArgumentGuard.NotNullOrEmpty(id);

        return DoGetInstanceAsync($"apps/{appName}/{id}", cancellationToken);
    }

    public async Task<EurekaHttpResponse> CancelAsync(string appName, string id, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrEmpty(appName);
        ArgumentGuard.NotNullOrEmpty(id);

        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        httpClient ??= GetHttpClient();

        // For retries
        for (int retry = 0; retry < GetRetryCount(); retry++)
        {
            string serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{appName}/{id}");
            HttpRequestMessage request = await GetRequestMessageAsync(HttpMethod.Delete, requestUri, cancellationToken);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
                _logger?.LogDebug("CancelAsync {RequestUri}, status: {StatusCode}, retry: {retry}", requestUri.ToMaskedString(), response.StatusCode, retry);
                Interlocked.Exchange(ref ServiceUrl, serviceUrl);

                return new EurekaHttpResponse(response.StatusCode, response.Headers);
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger?.LogError(exception, "CancelAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the CancelAsync request");
    }

    public async Task<EurekaHttpResponse> DeleteStatusOverrideAsync(string appName, string id, InstanceInfo info, CancellationToken cancellationToken)
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
        httpClient ??= GetHttpClient();

        // For retries
        for (int retry = 0; retry < GetRetryCount(); retry++)
        {
            string serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{appName}/{id}/status", queryArgs);
            HttpRequestMessage request = await GetRequestMessageAsync(HttpMethod.Delete, requestUri, cancellationToken);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

                _logger?.LogDebug("DeleteStatusOverrideAsync {RequestUri}, status: {StatusCode}, retry: {retry}", requestUri.ToMaskedString(),
                    response.StatusCode, retry);

                int statusCode = (int)response.StatusCode;

                if (statusCode >= 200 && statusCode < 300)
                {
                    Interlocked.Exchange(ref ServiceUrl, serviceUrl);

                    return new EurekaHttpResponse(response.StatusCode, response.Headers);
                }
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger?.LogError(exception, "DeleteStatusOverrideAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the DeleteStatusOverrideAsync request");
    }

    public async Task<EurekaHttpResponse> StatusUpdateAsync(string appName, string id, InstanceStatus newStatus, InstanceInfo info,
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
        httpClient ??= GetHttpClient();

        // For retries
        for (int retry = 0; retry < GetRetryCount(); retry++)
        {
            string serviceUrl = GetServiceUrl(candidateServiceUrls, ref index);
            Uri requestUri = GetRequestUri($"{serviceUrl}apps/{appName}/{id}/status", queryArgs);
            HttpRequestMessage request = await GetRequestMessageAsync(HttpMethod.Put, requestUri, cancellationToken);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

                _logger?.LogDebug("StatusUpdateAsync {RequestUri}, status: {StatusCode}, retry: {retry}", requestUri.ToMaskedString(), response.StatusCode,
                    retry);

                int statusCode = (int)response.StatusCode;

                if (statusCode >= 200 && statusCode < 300)
                {
                    Interlocked.Exchange(ref ServiceUrl, serviceUrl);

                    return new EurekaHttpResponse(response.StatusCode, response.Headers);
                }
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger?.LogError(exception, "StatusUpdateAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the StatusUpdateAsync request");
    }

    public Task ShutdownAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task<string> FetchAccessTokenAsync(CancellationToken cancellationToken)
    {
        return Configuration is not EurekaClientOptions options || string.IsNullOrEmpty(options.AccessTokenUri)
            ? null
            : await HttpClientHelper.GetAccessTokenAsync(options.AccessTokenUri, options.ClientId, options.ClientSecret,
                DefaultGetAccessTokenTimeoutInMilliseconds, options.ValidateCertificates, null, null, cancellationToken);
    }

    internal IList<string> GetServiceUrlCandidates()
    {
        // Get latest set of Eureka server urls
        IList<string> candidateServiceUrls = MakeServiceUrls(Configuration.EurekaServerServiceUrls);

        lock (_lock)
        {
            // Keep any existing failing service urls still in the candidate list
            _failingServiceUrls = _failingServiceUrls.Intersect(candidateServiceUrls).ToList();

            // If enough hosts are bad, we have no choice but start over again
            int threshold = (int)Math.Round(candidateServiceUrls.Count * 0.67);

            if (_failingServiceUrls.Count == 0)
            {
                // Intentionally left empty.
            }
            else if (_failingServiceUrls.Count >= threshold)
            {
                _logger?.LogDebug("Clearing quarantined list of size {Count}", _failingServiceUrls.Count);
                _failingServiceUrls.Clear();
            }
            else
            {
                var remainingHosts = new List<string>(candidateServiceUrls.Count);

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

        if (!string.IsNullOrEmpty(rawUserInfo) && rawUserInfo.IndexOfAny(ColonDelimiter) >= 0)
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

        request.Headers.Add("Accept", "application/json");
        request.Headers.Add(HttpXDiscoveryAllowRedirect, "false");
        return request;
    }

    protected internal Uri GetRequestUri(string baseUri, IDictionary<string, string> queryValues = null)
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

    private void Initialize(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory?.CreateLogger<EurekaHttpClient>();
        JsonSerializerOptions.Converters.Add(new JsonInstanceInfoConverter());

        // Validate serviceUrls
        MakeServiceUrls(Configuration.EurekaServerServiceUrls);
    }

    protected async Task<EurekaHttpResponse<InstanceInfo>> DoGetInstanceAsync(string path, CancellationToken cancellationToken)
    {
        IList<string> candidateServiceUrls = GetServiceUrlCandidates();
        int index = 0;
        httpClient ??= GetHttpClient();

        // For retries
        for (int retry = 0; retry < GetRetryCount(); retry++)
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

                _logger?.LogDebug("DoGetInstanceAsync {RequestUri}, status: {StatusCode}, instanceInfo: {Instance}, retry: {retry}",
                    requestUri.ToMaskedString(), response.StatusCode, infoResp != null ? infoResp.ToString() : "null", retry);

                int statusCode = (int)response.StatusCode;

                if ((statusCode >= 200 && statusCode < 300) || statusCode == 404)
                {
                    Interlocked.Exchange(ref ServiceUrl, serviceUrl);

                    return new EurekaHttpResponse<InstanceInfo>(response.StatusCode, response.Headers, infoResp);
                }
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger?.LogError(exception, "DoGetInstanceAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the DoGetInstanceAsync request");
    }

    protected async Task<EurekaHttpResponse<Applications>> DoGetApplicationsAsync(string path, ISet<string> regions,
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
        httpClient ??= GetHttpClient();

        // For retries
        for (int retry = 0; retry < GetRetryCount(); retry++)
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
                    _logger?.LogInformation(exception, "Failed to deserialize response");
                }

                Applications appsResp = null;

                if (response.StatusCode == HttpStatusCode.OK && root != null)
                {
                    appsResp = Applications.FromJsonApplications(root.Applications);
                }

                _logger?.LogDebug("DoGetApplicationsAsync {RequestUri}, status: {StatusCode}, applications: {Application}, retry: {retry}",
                    requestUri.ToMaskedString(), response.StatusCode, appsResp != null ? appsResp.ToString() : "null", retry);

                int statusCode = (int)response.StatusCode;

                if ((statusCode >= 200 && statusCode < 300) || statusCode == 404)
                {
                    Interlocked.Exchange(ref ServiceUrl, serviceUrl);

                    return new EurekaHttpResponse<Applications>(response.StatusCode, response.Headers, appsResp);
                }
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger?.LogError(exception, "DoGetApplicationsAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            Interlocked.CompareExchange(ref ServiceUrl, null, serviceUrl);
            AddToFailingServiceUrls(serviceUrl);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the DoGetApplicationsAsync request");
    }

    protected HttpClient GetHttpClient()
    {
        return httpClient ?? HttpClientHelper.GetHttpClient(Configuration.ValidateCertificates,
            ConfigureEurekaHttpClientHandler(Configuration, null), Configuration.EurekaServer.ConnectTimeoutSeconds * 1000);
    }

    internal static HttpClientHandler ConfigureEurekaHttpClientHandler(EurekaClientOptions configuration, HttpClientHandler handler)
    {
        handler ??= new HttpClientHandler();

        if (!string.IsNullOrEmpty(configuration.EurekaServer.ProxyHost))
        {
            handler.Proxy = new WebProxy(configuration.EurekaServer.ProxyHost, configuration.EurekaServer.ProxyPort);

            if (!string.IsNullOrEmpty(configuration.EurekaServer.ProxyPassword))
            {
                handler.Proxy.Credentials = new NetworkCredential(configuration.EurekaServer.ProxyUserName, configuration.EurekaServer.ProxyPassword);
            }
        }

        if (configuration.EurekaServer.ShouldGZipContent)
        {
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        }

        return handler;
    }

    protected HttpContent GetRequestContent(object toSerialize)
    {
        try
        {
            string json = JsonSerializer.Serialize(toSerialize);
            _logger?.LogDebug($"GetRequestContent generated JSON: {json}");
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception, "GetRequestContent Failed");
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
            result = userInfo.Split(ColonDelimiter);
        }

        return result;
    }

    private int GetRetryCount()
    {
        return Configuration.EurekaServer.RetryCount;
    }
}
