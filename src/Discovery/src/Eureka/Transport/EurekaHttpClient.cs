// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.Http;
using Steeltoe.Common.Util;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Util;

namespace Steeltoe.Discovery.Eureka.Transport;

public sealed class EurekaHttpClient
{
    private const string HttpXDiscoveryAllowRedirect = "X-Discovery-AllowRedirect";
    private static readonly TimeSpan GetAccessTokenTimeout = TimeSpan.FromSeconds(10);

    private readonly IOptionsMonitor<EurekaClientOptions> _optionsMonitor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EurekaServiceUriStateManager _eurekaServiceUriStateManager;
    private readonly ILogger<EurekaHttpClient> _logger;

    private static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonInstanceInfoConverter()
        }
    };

    public EurekaHttpClient(IOptionsMonitor<EurekaClientOptions> optionsMonitor, IHttpClientFactory httpClientFactory,
        EurekaServiceUriStateManager eurekaServiceUriStateManager, ILogger<EurekaHttpClient> logger)
    {
        ArgumentGuard.NotNull(optionsMonitor);
        ArgumentGuard.NotNull(httpClientFactory);
        ArgumentGuard.NotNull(eurekaServiceUriStateManager);
        ArgumentGuard.NotNull(logger);

        _optionsMonitor = optionsMonitor;
        _httpClientFactory = httpClientFactory;
        _eurekaServiceUriStateManager = eurekaServiceUriStateManager;
        _logger = logger;
    }

    public async Task<EurekaHttpResponse> RegisterAsync(InstanceInfo info, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(info);

        if ((Platform.IsContainerized || Platform.IsCloudHosted) && info.HostName?.Equals("localhost", StringComparison.OrdinalIgnoreCase) == true)
        {
            _logger?.LogWarning(
                "Registering with hostname 'localhost' in containerized or cloud environments may not be valid. Please configure Eureka:Instance:HostName with a non-localhost address");
        }

        using HttpClient httpClient = CreateHttpClient();
        EurekaServiceUriStateManager.ServiceUrisSnapshot serviceUris = _eurekaServiceUriStateManager.GetSnapshot();

        // For retries
        for (int retry = 0; retry < GetRetryCount(); retry++)
        {
            Uri serviceUri = serviceUris.GetNextServiceUri();
            Uri requestUri = GetRequestUri(serviceUri, $"apps/{info.AppName}");
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
                    _eurekaServiceUriStateManager.MarkWorkingServiceUri(serviceUri);

                    return new EurekaHttpResponse(response.StatusCode, response.Headers);
                }

                string jsonError = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger?.LogInformation("Failure during RegisterAsync: {jsonError}", jsonError);
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger?.LogError(exception, "RegisterAsync Failed, request was made to {requestUri}, retry: {retry}", requestUri.ToMaskedUri(), retry);
            }

            _eurekaServiceUriStateManager.MarkFailingServiceUri(serviceUri);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the RegisterAsync request");
    }

    public async Task<EurekaHttpResponse<InstanceInfo>> SendHeartBeatAsync(string appName, string id, InstanceInfo info, InstanceStatus overriddenStatus,
        CancellationToken cancellationToken)
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

        using HttpClient httpClient = CreateHttpClient();
        EurekaServiceUriStateManager.ServiceUrisSnapshot serviceUris = _eurekaServiceUriStateManager.GetSnapshot();

        for (int retry = 0; retry < GetRetryCount(); retry++)
        {
            Uri serviceUri = serviceUris.GetNextServiceUri();
            Uri requestUri = GetRequestUri(serviceUri, $"apps/{info.AppName}/{id}", queryArgs);
            HttpRequestMessage request = await GetRequestMessageAsync(HttpMethod.Put, requestUri, cancellationToken);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
                JsonInstanceInfo instanceInfo = null;

                try
                {
                    instanceInfo = await response.Content.ReadFromJsonAsync<JsonInstanceInfo>(SerializerOptions, cancellationToken);
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
                    _eurekaServiceUriStateManager.MarkWorkingServiceUri(serviceUri);

                    return new EurekaHttpResponse<InstanceInfo>(response.StatusCode, response.Headers, infoResp);
                }
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger?.LogError(exception, "SendHeartBeatAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            _eurekaServiceUriStateManager.MarkFailingServiceUri(serviceUri);
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

        using HttpClient httpClient = CreateHttpClient();
        EurekaServiceUriStateManager.ServiceUrisSnapshot serviceUris = _eurekaServiceUriStateManager.GetSnapshot();

        // For retries
        for (int retry = 0; retry < GetRetryCount(); retry++)
        {
            Uri serviceUri = serviceUris.GetNextServiceUri();
            Uri requestUri = GetRequestUri(serviceUri, $"apps/{appName}");

            HttpRequestMessage request = await GetRequestMessageAsync(HttpMethod.Get, requestUri, cancellationToken);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

                var applicationRoot = await response.Content.ReadFromJsonAsync<JsonApplicationRoot>(SerializerOptions, cancellationToken);

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
                    _eurekaServiceUriStateManager.MarkWorkingServiceUri(serviceUri);

                    return new EurekaHttpResponse<Application>(response.StatusCode, response.Headers, appResp);
                }
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger?.LogError(exception, "GetApplicationAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            _eurekaServiceUriStateManager.MarkFailingServiceUri(serviceUri);
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

        using HttpClient httpClient = CreateHttpClient();
        EurekaServiceUriStateManager.ServiceUrisSnapshot serviceUris = _eurekaServiceUriStateManager.GetSnapshot();

        // For retries
        for (int retry = 0; retry < GetRetryCount(); retry++)
        {
            Uri serviceUri = serviceUris.GetNextServiceUri();
            Uri requestUri = GetRequestUri(serviceUri, $"apps/{appName}/{id}");
            HttpRequestMessage request = await GetRequestMessageAsync(HttpMethod.Delete, requestUri, cancellationToken);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
                _logger?.LogDebug("CancelAsync {RequestUri}, status: {StatusCode}, retry: {retry}", requestUri.ToMaskedString(), response.StatusCode, retry);

                _eurekaServiceUriStateManager.MarkWorkingServiceUri(serviceUri);

                return new EurekaHttpResponse(response.StatusCode, response.Headers);
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger?.LogError(exception, "CancelAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            _eurekaServiceUriStateManager.MarkFailingServiceUri(serviceUri);
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

        using HttpClient httpClient = CreateHttpClient();
        EurekaServiceUriStateManager.ServiceUrisSnapshot serviceUris = _eurekaServiceUriStateManager.GetSnapshot();

        // For retries
        for (int retry = 0; retry < GetRetryCount(); retry++)
        {
            Uri serviceUri = serviceUris.GetNextServiceUri();
            Uri requestUri = GetRequestUri(serviceUri, $"apps/{appName}/{id}/status", queryArgs);
            HttpRequestMessage request = await GetRequestMessageAsync(HttpMethod.Delete, requestUri, cancellationToken);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

                _logger?.LogDebug("DeleteStatusOverrideAsync {RequestUri}, status: {StatusCode}, retry: {retry}", requestUri.ToMaskedString(),
                    response.StatusCode, retry);

                int statusCode = (int)response.StatusCode;

                if (statusCode >= 200 && statusCode < 300)
                {
                    _eurekaServiceUriStateManager.MarkWorkingServiceUri(serviceUri);

                    return new EurekaHttpResponse(response.StatusCode, response.Headers);
                }
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger?.LogError(exception, "DeleteStatusOverrideAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            _eurekaServiceUriStateManager.MarkFailingServiceUri(serviceUri);
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

        using HttpClient httpClient = CreateHttpClient();
        EurekaServiceUriStateManager.ServiceUrisSnapshot serviceUris = _eurekaServiceUriStateManager.GetSnapshot();

        // For retries
        for (int retry = 0; retry < GetRetryCount(); retry++)
        {
            Uri serviceUri = serviceUris.GetNextServiceUri();
            Uri requestUri = GetRequestUri(serviceUri, $"apps/{appName}/{id}/status", queryArgs);
            HttpRequestMessage request = await GetRequestMessageAsync(HttpMethod.Put, requestUri, cancellationToken);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

                _logger?.LogDebug("StatusUpdateAsync {RequestUri}, status: {StatusCode}, retry: {retry}", requestUri.ToMaskedString(), response.StatusCode,
                    retry);

                int statusCode = (int)response.StatusCode;

                if (statusCode >= 200 && statusCode < 300)
                {
                    _eurekaServiceUriStateManager.MarkWorkingServiceUri(serviceUri);

                    return new EurekaHttpResponse(response.StatusCode, response.Headers);
                }
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger?.LogError(exception, "StatusUpdateAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            _eurekaServiceUriStateManager.MarkFailingServiceUri(serviceUri);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the StatusUpdateAsync request");
    }

    internal async Task<HttpRequestMessage> GetRequestMessageAsync(HttpMethod method, Uri requestUri, CancellationToken cancellationToken)
    {
        var uriWithoutUserInfo = new Uri(requestUri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped));
        var requestMessage = new HttpRequestMessage(method, uriWithoutUserInfo);

        if (requestUri.TryGetUsernamePassword(out string username, out string password) && password.Length > 0)
        {
            requestMessage.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}")));
        }
        else
        {
            EurekaClientOptions options = _optionsMonitor.CurrentValue;

            if (!string.IsNullOrEmpty(options.AccessTokenUri))
            {
                using HttpClient httpClient = _httpClientFactory.CreateClient("EurekaAccessToken");

                string accessToken = await httpClient.GetAccessTokenAsync(new Uri(options.AccessTokenUri), options.ClientId, options.ClientSecret,
                    GetAccessTokenTimeout, cancellationToken);

                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        requestMessage.Headers.Add("Accept", "application/json");
        requestMessage.Headers.Add(HttpXDiscoveryAllowRedirect, "false");
        return requestMessage;
    }

#nullable enable
    private static Uri GetRequestUri(Uri baseUri, string path, IDictionary<string, string>? queryString = null)
    {
        var requestUri = new Uri(baseUri, path);

        if (queryString != null)
        {
            var uriBuilder = new UriBuilder(requestUri);
            var queryBuilder = new QueryBuilder(queryString);
            uriBuilder.Query = queryBuilder.ToQueryString().ToUriComponent();
            requestUri = uriBuilder.Uri;
        }

        return requestUri;
    }
#nullable disable

    private async Task<EurekaHttpResponse<InstanceInfo>> DoGetInstanceAsync(string path, CancellationToken cancellationToken)
    {
        using HttpClient httpClient = CreateHttpClient();
        EurekaServiceUriStateManager.ServiceUrisSnapshot serviceUris = _eurekaServiceUriStateManager.GetSnapshot();

        // For retries
        for (int retry = 0; retry < GetRetryCount(); retry++)
        {
            Uri serviceUri = serviceUris.GetNextServiceUri();
            Uri requestUri = GetRequestUri(serviceUri, path);
            HttpRequestMessage request = await GetRequestMessageAsync(HttpMethod.Get, requestUri, cancellationToken);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
                var infoRoot = await response.Content.ReadFromJsonAsync<JsonInstanceInfoRoot>(SerializerOptions, cancellationToken);

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
                    _eurekaServiceUriStateManager.MarkWorkingServiceUri(serviceUri);

                    return new EurekaHttpResponse<InstanceInfo>(response.StatusCode, response.Headers, infoResp);
                }
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger?.LogError(exception, "DoGetInstanceAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            _eurekaServiceUriStateManager.MarkFailingServiceUri(serviceUri);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the DoGetInstanceAsync request");
    }

    private async Task<EurekaHttpResponse<Applications>> DoGetApplicationsAsync(string path, ISet<string> regions, CancellationToken cancellationToken)
    {
        string regionParams = CommaDelimit(regions);

        var queryArgs = new Dictionary<string, string>();

        if (regionParams != null)
        {
            queryArgs.Add("regions", regionParams);
        }

        using HttpClient httpClient = CreateHttpClient();
        EurekaServiceUriStateManager.ServiceUrisSnapshot serviceUris = _eurekaServiceUriStateManager.GetSnapshot();

        // For retries
        for (int retry = 0; retry < GetRetryCount(); retry++)
        {
            Uri serviceUri = serviceUris.GetNextServiceUri();
            Uri requestUri = GetRequestUri(serviceUri, path, queryArgs);
            HttpRequestMessage request = await GetRequestMessageAsync(HttpMethod.Get, requestUri, cancellationToken);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
                JsonApplicationsRoot root = null;

                try
                {
                    root = await response.Content.ReadFromJsonAsync<JsonApplicationsRoot>(SerializerOptions, cancellationToken);
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
                    _eurekaServiceUriStateManager.MarkWorkingServiceUri(serviceUri);

                    return new EurekaHttpResponse<Applications>(response.StatusCode, response.Headers, appsResp);
                }
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger?.LogError(exception, "DoGetApplicationsAsync Failed, request was made to {requestUri}", requestUri.ToMaskedUri());
            }

            _eurekaServiceUriStateManager.MarkFailingServiceUri(serviceUri);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the DoGetApplicationsAsync request");
    }

    private HttpClient CreateHttpClient()
    {
        HttpClient httpClient = _httpClientFactory.CreateClient("Eureka");

        int connectTimeoutSeconds = _optionsMonitor.CurrentValue.EurekaServer.ConnectTimeoutSeconds;

        if (connectTimeoutSeconds > 0)
        {
            httpClient.Timeout = TimeSpan.FromSeconds(connectTimeoutSeconds);
        }

        return httpClient;
    }

    private HttpContent GetRequestContent(object toSerialize)
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

    private int GetRetryCount()
    {
        EurekaClientOptions clientOptionsSnapshot = _optionsMonitor.CurrentValue;
        return clientOptionsSnapshot.EurekaServer.RetryCount;
    }
}
