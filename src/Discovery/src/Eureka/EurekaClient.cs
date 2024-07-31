// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.Http;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Sends HTTP requests to Eureka servers.
/// </summary>
public sealed class EurekaClient
{
    // HTTP endpoints are described at: https://github.com/Netflix/eureka/wiki/Eureka-REST-operations
    // Self preservation is described at: https://www.baeldung.com/eureka-self-preservation-renewal
    // Health monitoring is described at: https://medium.com/@fahimfarookme/the-mystery-of-eureka-health-monitoring-fd05fe757928
    // Eureka timing settings are described at: https://blogs.asarkar.com/technical/netflix-eureka/

    private const string MediaType = "application/json";
    private const string DiscoveryAllowRedirectHeaderName = "X-Discovery-AllowRedirect";
    private static readonly Task<object?> TaskOfNull = Task.FromResult<object?>(null);
    private static readonly TimeSpan GetAccessTokenTimeout = TimeSpan.FromSeconds(10);

    private static readonly JsonSerializerOptions RequestSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions ResponseSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonApplicationConverter(),
            new JsonInstanceInfoConverter()
        }
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<EurekaClientOptions> _optionsMonitor;
    private readonly EurekaServiceUriStateManager _eurekaServiceUriStateManager;
    private readonly ILogger<EurekaClient> _logger;

    public EurekaClient(IHttpClientFactory httpClientFactory, IOptionsMonitor<EurekaClientOptions> optionsMonitor,
        EurekaServiceUriStateManager eurekaServiceUriStateManager, ILogger<EurekaClient> logger)
    {
        ArgumentGuard.NotNull(httpClientFactory);
        ArgumentGuard.NotNull(optionsMonitor);
        ArgumentGuard.NotNull(eurekaServiceUriStateManager);
        ArgumentGuard.NotNull(logger);

        _httpClientFactory = httpClientFactory;
        _optionsMonitor = optionsMonitor;
        _eurekaServiceUriStateManager = eurekaServiceUriStateManager;
        _logger = logger;
    }

    /// <summary>
    /// Registers an application instance.
    /// </summary>
    /// <param name="instance">
    /// The instance to register.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="EurekaTransportException">
    /// The operation failed because none of the Eureka servers responded with success.
    /// </exception>
    public async Task RegisterAsync(InstanceInfo instance, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(instance);

        if ((Platform.IsContainerized || Platform.IsCloudHosted) && string.Equals(instance.HostName, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Registering with hostname 'localhost' in containerized or cloud environments may not be valid. " +
                "Please configure Eureka:Instance:HostName with a non-localhost address.");
        }

        string requestBody = JsonSerializer.Serialize(new JsonInstanceInfoRoot
        {
            Instance = instance.ToJson()
        }, RequestSerializerOptions);

        string path = $"apps/{WebUtility.UrlEncode(instance.AppName)}";
        await ExecuteRequestAsync(HttpMethod.Post, path, null, requestBody, cancellationToken);
    }

    /// <summary>
    /// Deregisters an application instance.
    /// </summary>
    /// <param name="appName">
    /// The name of the app to deregister.
    /// </param>
    /// <param name="instanceId">
    /// The ID of the instance to deregister.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="EurekaTransportException">
    /// The operation failed because none of the Eureka servers responded with success.
    /// </exception>
    public async Task DeregisterAsync(string appName, string instanceId, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrWhiteSpace(appName);
        ArgumentGuard.NotNullOrWhiteSpace(instanceId);

        string path = $"apps/{WebUtility.UrlEncode(appName)}/{WebUtility.UrlEncode(instanceId)}";
        await ExecuteRequestAsync(HttpMethod.Delete, path, null, null, cancellationToken);
    }

    /// <summary>
    /// Sends a heartbeat for an application instance.
    /// </summary>
    /// <param name="appName">
    /// The name of the app to send a heartbeat for.
    /// </param>
    /// <param name="instanceId">
    /// The ID of the instance to send a heartbeat for.
    /// </param>
    /// <param name="lastDirtyTimeUtc">
    /// The date and time (in UTC) when the instance was last dirty.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="EurekaTransportException">
    /// The operation failed because none of the Eureka servers responded with success.
    /// </exception>
    public async Task HeartbeatAsync(string appName, string instanceId, DateTime? lastDirtyTimeUtc, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrWhiteSpace(appName);
        ArgumentGuard.NotNullOrWhiteSpace(instanceId);

        // NOTES:
        // - The 'status' query string parameter is always ignored by Eureka Server.
        // - The 'overriddenStatus' query string parameter is only used in Eureka server-to-server scenarios.
        // See InstanceResource.renewLease() at https://github.com/Netflix/eureka/blob/master/eureka-core/src/main/java/com/netflix/eureka/resources/InstanceResource.java#L105-L110.

        // A Eureka server returns 404 when our lastDirtyTimeUtc is newer, then it wants us to re-register because it believes to be outdated.
        // This can happen in a cluster of Eureka servers where not all servers are in sync.
        // Because we're sequentially sending a heartbeat to all known servers until one succeeds, we leave it up to the servers
        // to keep each other in sync. So the caller of this method should only try to re-register when none of the Eureka servers reported success.

        Dictionary<string, string>? queryString = lastDirtyTimeUtc != null
            ? new Dictionary<string, string>
            {
                ["lastDirtyTimestamp"] = DateTimeConversions.ToJavaMilliseconds(lastDirtyTimeUtc.Value).ToString(CultureInfo.InvariantCulture)
            }
            : null;

        string path = $"apps/{WebUtility.UrlEncode(appName)}/{WebUtility.UrlEncode(instanceId)}";
        await ExecuteRequestAsync(HttpMethod.Put, path, queryString, null, cancellationToken);
    }

    /// <summary>
    /// Queries for all application instances.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="EurekaTransportException">
    /// The operation failed because none of the Eureka servers responded with success.
    /// </exception>
    public Task<ApplicationInfoCollection> GetApplicationsAsync(CancellationToken cancellationToken)
    {
        return GetApplicationsAtPathAsync("apps", cancellationToken);
    }

    /// <summary>
    /// Queries for a delta of all application instances.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="EurekaTransportException">
    /// The operation failed because none of the Eureka servers responded with success.
    /// </exception>
    public Task<ApplicationInfoCollection> GetDeltaAsync(CancellationToken cancellationToken)
    {
        return GetApplicationsAtPathAsync("apps/delta", cancellationToken);
    }

    /// <summary>
    /// Queries for all application instances under a particular VIP address.
    /// </summary>
    /// <param name="vipAddress">
    /// The Virtual Internet Protocol address whose instances to return.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="EurekaTransportException">
    /// The operation failed because none of the Eureka servers responded with success.
    /// </exception>
    public Task<ApplicationInfoCollection> GetByVipAsync(string vipAddress, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrWhiteSpace(vipAddress);

        string path = $"vips/{WebUtility.UrlEncode(vipAddress)}";
        return GetApplicationsAtPathAsync(path, cancellationToken);
    }

    private async Task<ApplicationInfoCollection> GetApplicationsAtPathAsync(string path, CancellationToken cancellationToken)
    {
        return await ExecuteRequestAsync(HttpMethod.Get, path, null, null, async response =>
        {
            var root = await response.Content.ReadFromJsonAsync<JsonApplicationsRoot>(ResponseSerializerOptions, cancellationToken);
            return ApplicationInfoCollection.FromJson(root?.Applications);
        }, cancellationToken);
    }

    private async Task ExecuteRequestAsync(HttpMethod method, string path, IDictionary<string, string>? queryString, string? requestBody,
        CancellationToken cancellationToken)
    {
        _ = await ExecuteRequestAsync(method, path, queryString, requestBody, _ => TaskOfNull, cancellationToken);
    }

    private async Task<TResult> ExecuteRequestAsync<TResult>(HttpMethod method, string path, IDictionary<string, string>? queryString, string? requestBody,
        Func<HttpResponseMessage, Task<TResult>> getResultAsync, CancellationToken cancellationToken)
    {
        EurekaClientOptions clientOptions = _optionsMonitor.CurrentValue;
        TimeSpan connectTimeout = TimeSpan.FromSeconds(clientOptions.EurekaServer.ConnectTimeoutSeconds);

        using HttpClient httpClient = CreateHttpClient("Eureka", connectTimeout);
        EurekaServiceUriStateManager.ServiceUrisSnapshot serviceUris = _eurekaServiceUriStateManager.GetSnapshot();

        int tryCount = clientOptions.EurekaServer.RetryCount + 1;

        for (int attempt = 1; attempt <= tryCount; attempt++)
        {
            Uri serviceUri = serviceUris.GetNextServiceUri();
            Uri requestUri = GetRequestUri(serviceUri, path, queryString);

            HttpContent? requestContent = requestBody != null ? new StringContent(requestBody, Encoding.UTF8, MediaType) : null;
            HttpRequestMessage request = await GetRequestMessageAsync(method, requestUri, requestContent, cancellationToken);

            if (!string.IsNullOrEmpty(requestBody))
            {
                _logger.LogDebug("Sending {RequestMethod} request to '{RequestUri}' with body: {RequestBody}.", request.Method, requestUri.ToMaskedString(),
                    requestBody);
            }
            else
            {
                _logger.LogDebug("Sending {RequestMethod} request to '{RequestUri}' without request body.", request.Method, requestUri.ToMaskedString());
            }

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

                _logger.LogDebug("HTTP {RequestMethod} request to '{RequestUri}' returned status {StatusCode} in attempt {Attempt}.", request.Method,
                    requestUri.ToMaskedString(), (int)response.StatusCode, attempt);

                if (response.IsSuccessStatusCode)
                {
                    _eurekaServiceUriStateManager.MarkWorkingServiceUri(serviceUri);

                    try
                    {
                        return await getResultAsync(response);
                    }
                    catch (JsonException exception) when (!exception.IsCancellation())
                    {
                        _logger.LogDebug(exception, "Failed to deserialize HTTP response from {RequestMethod} '{RequestUri}'.", request.Method,
                            requestUri.ToMaskedString());
                    }
                }
                else
                {
                    string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                    _logger.LogInformation("HTTP {RequestMethod} request to '{RequestUri}' failed with status {StatusCode}: {ResponseBody}", request.Method,
                        requestUri.ToMaskedString(), (int)response.StatusCode, responseBody);
                }
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger.LogWarning(exception, "Failed to execute HTTP {RequestMethod} request to '{RequestUri}' in attempt {Attempt}.", request.Method,
                    requestUri.ToMaskedString(), attempt);
            }

            _eurekaServiceUriStateManager.MarkFailingServiceUri(serviceUri);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the HTTP request.");
    }

    private HttpClient CreateHttpClient(string name, TimeSpan connectTimeout)
    {
        HttpClient httpClient = _httpClientFactory.CreateClient(name);
        httpClient.ConfigureForSteeltoe(connectTimeout);
        return httpClient;
    }

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

    private async Task<HttpRequestMessage> GetRequestMessageAsync(HttpMethod method, Uri requestUri, HttpContent? content, CancellationToken cancellationToken)
    {
        var uriWithoutUserInfo = new Uri(requestUri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped));
        var requestMessage = new HttpRequestMessage(method, uriWithoutUserInfo);

        if (requestUri.TryGetUsernamePassword(out string? username, out string? password) && password.Length > 0)
        {
            _logger.LogDebug("Adding credentials from '{RequestUri}' to Authorization header.", requestUri.ToMaskedString());

            requestMessage.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}")));
        }
        else
        {
            EurekaClientOptions clientOptions = _optionsMonitor.CurrentValue;

            if (!string.IsNullOrEmpty(clientOptions.AccessTokenUri))
            {
                using HttpClient httpClient = CreateHttpClient("AccessTokenForEureka", GetAccessTokenTimeout);
                var accessTokenUri = new Uri(clientOptions.AccessTokenUri);

                string accessToken = await httpClient.GetAccessTokenAsync(accessTokenUri, clientOptions.ClientId,
                    clientOptions.ClientSecret, cancellationToken);

                _logger.LogDebug("Fetched access token from '{AccessTokenUri}'.", accessTokenUri.ToMaskedString());
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        requestMessage.Headers.Add("Accept", MediaType);
        requestMessage.Headers.Add(DiscoveryAllowRedirectHeaderName, "false");

        requestMessage.Content = content;

        return requestMessage;
    }
}
